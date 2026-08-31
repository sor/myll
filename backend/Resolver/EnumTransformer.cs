using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Pre-resolution transform for enumeration attributes.
	///
	/// - <c>[flags]</c>: auto-number entries as powers of two.
	/// - <c>[operators(bitwise)]</c>: synthesize <c>operator&amp;|~^</c> and compound
	///   assignment operators in the nearest enclosing namespace.
	/// </summary>
	public sealed class EnumTransformer : ITransformer
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

		public void Transform( IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			foreach( (GlobalNamespace module, _) in modules )
				TransformDecl( module );
		}

		private static void TransformDecl( Decl decl )
		{
			if( decl is Enumeration enumeration )
				TransformEnumeration( enumeration );

			if( decl is Hierarchical h ) {
				// Snapshot the children because synthesizing enum operators can add
				// new declarations to the same namespace we are currently iterating.
				foreach( Decl child in h.children.ToList() )
					TransformDecl( child );
			}
		}

		private static void TransformEnumeration( Enumeration enumeration )
		{
			if( enumeration.IsFlags )
				NumberFlags( enumeration );

			if( enumeration.IsOpBitwise )
				SynthesizeBitwiseOperators( enumeration );
		}

		private static void NumberFlags( Enumeration enumeration )
		{
			uint index = 1;
			foreach( Decl child in enumeration.children ) {
				if( child is not EnumEntry ee )
					continue;

				if( ee.value is Literal lit ) {
					uint readIndex = uint.Parse( lit.text );
					index = readIndex == 0 ? 1 : readIndex * 2;
				}
				else {
					if( !IsPowerOfTwo( index ) )
						throw new Exception(
							String.Format(
								"'[flags]enum' auto numbering not a power of two: {0} at {1}",
								index,
								ee.srcPos ) );

					ee.value = new Literal { op = Operand.Literal, text = index.ToString() };
					index   *= 2;
				}
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

		private static bool IsPowerOfTwo( uint x )
			=> (x & (x - 1)) == 0;
	}
}
