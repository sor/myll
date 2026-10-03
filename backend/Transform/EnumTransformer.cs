using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Myll.Core;
using Myll.Resolver;

namespace Myll.Transform
{
	/// <summary>
	/// Pre-resolution transform for enumeration attributes.
	///
	/// - <c>[flags]</c>: auto-number entries as powers of two.
	/// - <c>[operators(bitwise)]</c>: synthesize <c>operator&amp;|~^</c> and compound
	///   assignment operators in the nearest enclosing namespace.
	/// </summary>
	public sealed class EnumTransformer : Transformer
	{
		private static readonly (string, Operand)[] BitwiseOps = {
			("operator&", Operand.BitAnd),
			("operator|", Operand.BitOr),
			("operator^", Operand.BitXor),
		};

		private static readonly (string, Operand)[] BitwiseEqualOps = {
			("operator&=", Operand.BitAnd),
			("operator|=", Operand.BitOr),
			("operator^=", Operand.BitXor),
		};

		protected override void PreTransform( TransformContext ctx )
		{
			Checker walker = new( Diagnostics );
			walker.WalkModule( ctx.Module.Module );
		}

		private sealed class Checker : DeclWalker
		{
			private readonly List<Diagnostic> diagnostics;

			public Checker( List<Diagnostic> diagnostics ) => this.diagnostics = diagnostics;

			protected override void TransformDecl( Decl decl )
			{
				if( decl is Enumeration enumeration )
					TransformEnumeration( enumeration, diagnostics );

				base.TransformDecl( decl );
			}
		}

		private static void TransformEnumeration( Enumeration enumeration, List<Diagnostic> diagnostics )
		{
			if( enumeration.IsFlags )
				NumberFlags( enumeration, diagnostics );

			if( enumeration.IsOpBitwise )
				SynthesizeBitwiseOperators( enumeration );
		}

		private static void NumberFlags( Enumeration enumeration, List<Diagnostic> diagnostics )
		{
			// Auto numbering only applies to entries without a value. A literal value
			// advances the doubling chain (validated to the u32 range). Computed
			// values (e.g. `Yellow = Red | Green`) are left untouched and emitted
			// as-is; they are transparent to the chain and C++ validates them as
			// constant expressions.
			BigInteger index = 1;
			foreach( Decl child in enumeration.children ) {
				if( child is not EnumEntry ee )
					continue;

				if( ee.value == null ) {
					if( !IsPowerOfTwo( index ) ) {
						diagnostics.Add( new Diagnostic(
							ee.srcPos,
							DiagnosticKind.Error,
							String.Format(
								"'[flags]enum' auto numbering not a power of two: {0}",
								index ) ) );
						return;
					}

					ee.value = new Literal { op = Operand.Literal, text = index.ToString() };
					index *= 2;
					continue;
				}

				// A negative literal is spelled as a unary minus over a literal.
				string? literalText = null;
				if( ee.value is Literal lit )
					literalText = lit.text;
				else if( ee.value is UnOp { op: Operand.PreMinus, expr: Literal negated } )
					literalText = "-" + negated.text;

				// Computed values are left untouched; the chain keeps doubling.
				if( literalText == null )
					continue;

				// Hex and binary literals are legal here; anything unparsable,
				// negative, or above the u32 range is a clean diagnostic, not a crash.
				if( !ConversionRules.TryParseInteger( literalText, out BigInteger readValue )
				 || readValue.Sign < 0
				 || readValue > (BigInteger) uint.MaxValue ) {
					diagnostics.Add( new Diagnostic(
						ee.srcPos,
						DiagnosticKind.Error,
						String.Format(
							"'[flags]enum' value must be a non-negative integer that fits in a u32: {0}",
							literalText ) ) );
					return;
				}

				index = readValue == 0 ? 1 : readValue * 2;
			}
		}

		private static void SynthesizeBitwiseOperators( Enumeration enumeration )
		{
			bool  isInlined  = enumeration.IsInlined;
			Scope namespaceUp = enumeration.scope.UpToNamespace;

			TypespecNested
				enumTypespec = new() {
					idTpls = new() {
						new() { id = enumeration.FullyQualifiedName },
					},
				},
				enumTypespecRef = new() {
					ptrs   = new() { new() { kind = Pointer.Kind.LVRef } },
					idTpls = new() {
						new() { id = enumeration.FullyQualifiedName },
					},
				};

			TypespecNested underlying = new() {
				idTpls = new() {
					new() { id = "std" },
					new() { id = "underlying_type", tplArgs = new() { new() { typespec = enumTypespec } } },
					new() { id = "type" },
				},
			};

			foreach( (string name, Operand op) in BitwiseOps ) {
				Expr lhs = ParamId( "lhs" );
				Expr rhs = ParamId( "rhs" );

				Func ret = new() {
					srcPos    = enumeration.srcPos,
					name      = name,
					TplParams = new(),
					retType   = enumTypespec,
					paras = new() {
						new() { name = "lhs", type = enumTypespec },
						new() { name = "rhs", type = enumTypespec },
					},
					body = new ReturnStmt {
						srcPos = enumeration.srcPos,
						expr = new CastExpr {
							op   = Operand.StaticCast,
							type = enumTypespec,
							expr = new BinOp {
								op = op,
								left = new CastExpr {
									op   = Operand.StaticCast,
									type = underlying,
									expr = lhs,
								},
								right = new CastExpr {
									op   = Operand.StaticCast,
									type = underlying,
									expr = rhs,
								},
							},
						},
					}.ToBlock(),
				};

				ret.AssignAttribs(
					isInlined
						? new() { { "ct", new() }, { "inline", new() } }
						: new() { { "ct", new() } } );

				AddToNamespace( namespaceUp, ret );
			}

			foreach( (string name, Operand op) in BitwiseEqualOps ) {
				Expr lhs = ParamId( "lhs" );
				Expr rhs = ParamId( "rhs" );

				Func ret = new() {
					srcPos    = enumeration.srcPos,
					name      = name,
					TplParams = new(),
					retType   = enumTypespecRef,
					paras = new() {
						new() { name = "lhs", type = enumTypespecRef },
						new() { name = "rhs", type = enumTypespec },
					},
					body = new List<Stmt> {
						new MultiAssign {
							srcPos = enumeration.srcPos,
							exprs = new() {
								lhs,
								new CastExpr {
									op   = Operand.StaticCast,
									type = enumTypespec,
									expr = new BinOp {
										op = op,
										left = new CastExpr {
											op   = Operand.StaticCast,
											type = underlying,
											expr = lhs,
										},
										right = new CastExpr {
											op   = Operand.StaticCast,
											type = underlying,
											expr = rhs,
										},
									},
								},
							},
						},
						new ReturnStmt {
							srcPos = enumeration.srcPos,
							expr   = lhs,
						},
					}.ToBlock(),
				};

				if( isInlined )
					ret.AssignAttribs( new() { { "inline", new() } } );

				AddToNamespace( namespaceUp, ret );
			}
		}

		private static IdExpr ParamId( string name )
			=> new() { op = Operand.Id, idTplArgs = new() { id = name } };

		private static void AddToNamespace( Scope namespaceScope, Func func )
			=> namespaceScope.AddChild( new ScopeLeaf { parent = namespaceScope, decl = func } );

		private static bool IsPowerOfTwo( BigInteger x )
			=> x > 0 && ( x & ( x - 1 ) ).IsZero;
	}
}
