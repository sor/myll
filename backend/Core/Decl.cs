﻿//#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Myll.Generator;

namespace Myll.Core
{
	using static String;

	using Strings = List<string>;
	using Attribs = Dictionary<string, List<string>>;

	public enum Access
	{
		Private,
		Protected,
		Public,
	}

	interface ITplParams
	{
		List<TplParam> TplParams { get; }
	}

	/// <summary>
	/// introduces a name (most of the time)
	/// a Decl is a Stmt, do not question this for now
	/// </summary>
	public abstract class Decl : Stmt
	{
		public string    name { get; init; }
		public Access    access = Access.Public;
		public ScopeLeaf scope;

		// recursively check if there is anything templated surrounding
		public bool IsTemplateUp {
			get {
				for( ScopeLeaf cur = scope; cur?.parent != null; cur = cur.parent )
					if( cur.decl is ITplParams curStruct )
						if( curStruct.TplParams.Count >= 1 )
							return true;

				return false;
			}
		}

		public bool IsExternal => HasAttrib( "extern" );
		public bool IsInline   => HasAttrib( "inline" ) || IsTemplateUp;
		public bool IsInStruct => scope.parent.decl is Structural;

		// TODO Symbol?

		public override string ToString()
		{
			StringBuilder sb = new();
			foreach( var info in GetType().GetProperties() ) {
				object value = info.GetValue( this, null )
				            ?? "(null)";
				sb.Append( info.Name + ": " + value + ", " );
			}

			sb.Length = Math.Max( sb.Length - 2, 0 );
			return "{"
			     + GetType().Name + " '"
			     + name           + "' "
			     + sb             + "}";
		}

		public string FullyQualifiedName {
			get {
				Strings ret = new();
				for( ScopeLeaf cur = scope; cur?.parent != null; cur = cur.parent ) {
					Decl   decl     = cur.decl;
					string declName = decl?.name ?? "unknown_fix_me";
					if( decl is ITplParams curStruct )
						if( curStruct.TplParams.Count >= 1 )
							declName += "<" + curStruct.TplParams
								.Select( t => t.name )
								.Join( ", " ) + ">";
					ret.Add( declName );
				}
				// WTF dot net framework?
				return ((IEnumerable<string>) ret).Reverse().Join( "::" );
			}
		}

		public abstract void AddToGen( HierarchicalGen gen );

		// Shouldn't this be abstract?
		public override Strings Gen( int level )
		{
			throw new NotImplementedException(
				Format(
					"plx implement in missing class: {0}",
					GetType().Name ) );
		}
	}

	// Has an in-order list of decls, visible from outside
	public abstract class Hierarchical : Decl
	{
		public readonly List<Decl> children = new();

		public new Scope scope {
			get => base.scope as Scope;
			set => base.scope = value;
		}

		public virtual Access defaultAccess => Access.Public;

		// the children add themselves through AddChild or PushScope
		public void AddChild( Decl decl )
		{
			children.Add( decl );
		}
	}

	// functions, methods, operators, accessors (in the end)
	public class Func : Decl, ITplParams
	{
		public enum Kind
		{
			Function,
			Procedure,
			Method,
			Operator, // free or bound to obj
			Convert,
		}

		public Kind                 kind;
		public List<TplParam>       TplParams { get; init; } = new();
		public List<TypespecNested> Requires  { get; init; } = new(); // TODO: replace with dedicated type
		public List<Param>          paras;
		public MultiStmt?           body; // isScope = true
		public Typespec             retType;

		public bool IsVirtual  => HasAttrib( "virtual" );
		public bool IsConst    => HasAttrib( "const" ) || HasAttrib( "pure" );
		public bool IsOverride => HasAttrib( "override" );

		// TODO: analyze, for void or auto return type of funcs
		public bool IsReturningSomething => false;

		public override void AddToGen( HierarchicalGen gen )
		{
			// null body could be used for extern stuff
			if( body == null && !IsExternal )
				throw new Exception( "Func has body: null" );

			gen.AddFunc( this );
		}
	}

	// Constructor / Destructor
	public class Structor : Decl
	{
		public enum Kind
		{
			Constructor,
			Destructor,
		}

		public Kind        kind;
		public List<Param> paras;
		public MultiStmt?  body; // isScope = true

		public bool IsVirtual  => HasAttrib( "virtual" );
		public bool IsImplicit => HasAttrib( "implicit" );
		public bool IsDefault  => HasAttrib( "default" );
		public bool IsDisabled => HasAttrib( "disable" );

		// TODO: initlist

		public override void AddToGen( HierarchicalGen gen )
		{
			if( body == null && !IsDefault && !IsDisabled )
				throw new Exception( "Structor with no body needs either [default] or [disable](delete in C++)" );

			gen.AddStructor( this );
		}
	}

	public class UsingDecl : Decl
	{
		// in locations where C++ does not support "using (namespace)" this must not be printed
		// but instead the unqualified types need to be changed to qualified ones
		//public List<TypespecNested> types;
		// TODO: add distinction to alias and using ns decls
		public Typespec type;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddUsing( this );
		}
	}

	/**
	<remarks>
		This needs to know if it needs to output "typename" in front of the type.
		This needs to have been created by a var or field decl,
			or from <see cref="Operand.WildId"/> and <see cref="Operand.DiscardId"/> in an earlier step
	</remarks>
	<example>
		var int i { [inline] get; [inline] set; } = 99;
	</example>
	*/
	public class VarDecl : Decl
	{
		public enum Kind
		{
			Var,
			Field,
			Const,
			Let,
		}

		public Kind           kind;
		public Typespec       type;     // contains Qualifier
		public List<Accessor> accessor; // opt, structural or global
		public Expr?          init;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddVar( this );
		}
	}

	public class MultiDecl : Decl
	{
		public List<Decl> decls = new();

		public MultiDecl() {}
		public MultiDecl( IEnumerable<Decl>? decls )
		{
			// TODO: if decls contains MultiDecl then unwrap them
			this.decls = decls?.ToList() ?? new();
		}

		public override void AssignAttribs( Attribs inAttribs )
		{
			decls.ForEach( v => v.AssignAttribs( inAttribs ) );
		}

		public override void AddToGen( HierarchicalGen gen )
		{
			decls.ForEach( v => v.AddToGen( gen ) );
		}

		public override Strings Gen( int level )
		{
			return decls.SelectMany( v => v.Gen( level ) ).ToList();
		}
	}

	public class EnumEntry : Decl
	{
		public Expr? value;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddEntry( this );
		}
	}

	public class Enumeration : Hierarchical
	{
		public TypespecBasic baseType;

		public bool IsFlags     => HasAttrib( "flags" );
		public bool IsOpBitwise => IsAttrib( "operators", "bitwise" );

		// TODO this needs to be in the Generator Folder
		static readonly (string, Operand)[] BitwiseOps = {
			("operator&", Operand.BitAnd),
			("operator|", Operand.BitOr),
			("operator^", Operand.BitXor),
		};

		static readonly (string, Operand)[] BitwiseEqualOps = {
			("operator&=", Operand.BitAnd),
			("operator|=", Operand.BitOr),
			("operator^=", Operand.BitXor),
		};

		// 0 will return true as well, which is maybe not what was expected
		bool IsPowerOfTwo( uint x )
		{
			bool ret = (x & (x - 1)) == 0;
			return ret;
		}

		// TODO this needs to be in the Generator Folder
		protected override void AttribsAssigned()
		{
			if( IsFlags ) {
				// HACK: This is just written in a hurry, might break at any user intervention
				uint index = 1;
				foreach( Decl child in children ) {
					if( child is EnumEntry ee ) {
						if( ee.value is Literal lit ) {
							uint readIndex = uint.Parse( lit.text );
							if( readIndex == 0 )
								index = 1;
							else
								index = readIndex * 2;
						}
						else {
							if( !IsPowerOfTwo( index ) )
								throw new Exception(
									Format(
										"'[flags]enum' auto numbering not a power of two: {0} at {1}",
										index,
										ee.srcPos ) );
							ee.value =  new Literal { op = Operand.Literal, text = index.ToString() };
							index    *= 2;
						}
					}
				}
			}

			if( IsOpBitwise ) {
				bool  isInline    = IsInline;
				Scope namespaceUp = scope.UpToNamespace;

				// HACK: this should work for most, but is an incorrect Typespec
				TypespecNested
					enumTypespec = new() {
						idTpls = new() {
							new() { id = FullyQualifiedName },
						},
					},
					enumTypespecRef = new() {
						ptrs   = new() { new() { kind = Pointer.Kind.LVRef } },
						idTpls = new() { new() { id   = FullyQualifiedName } },
					};

				TypespecNested underlying = new() {
					idTpls = new() {
						new() { id = "std" },
						new() { id = "underlying_type", tplArgs = new() { new() { typespec = enumTypespec } } },
						new() { id = "type" },
					},
				};

				Expr
					lhs = new IdExpr { op = Operand.Id, idTplArgs = new() { id = "lhs" }, },
					rhs = new IdExpr { op = Operand.Id, idTplArgs = new() { id = "rhs" }, };

				foreach( (string, Operand) tuple in BitwiseOps ) {
					Func ret = new() {
						srcPos    = srcPos, // TODO should be the pos of the attribute
						name      = tuple.Item1,
						TplParams = new(),
						retType   = enumTypespec,
						paras = new() {
							new() { name = "lhs", type = enumTypespec },
							new() { name = "rhs", type = enumTypespec },
						},
						body = new ReturnStmt {
							expr = new CastExpr {
								op   = Operand.StaticCast,
								type = enumTypespec,
								expr = new BinOp {
									op = tuple.Item2,
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
						isInline
							? new() { { "ct", new() }, { "inline", new() } }
							: new() { { "ct", new() } } );

					ScopeLeaf scopeLeaf = new() {
						parent = namespaceUp,
						decl   = ret,
					};
					// can not directly put these in an enumeration or structural (maybe as friend?)
					namespaceUp.AddChild( scopeLeaf );
				}

				foreach( (string, Operand) tuple in BitwiseEqualOps ) {
					Func ret = new() {
						srcPos    = srcPos, // TODO should be the pos of the attribute
						name      = tuple.Item1,
						TplParams = new(),
						retType   = enumTypespecRef,
						paras = new() {
							new() { name = "lhs", type = enumTypespecRef },
							new() { name = "rhs", type = enumTypespec },
						},
						body = new List<Stmt> {
							new MultiAssign {
								srcPos = srcPos,
								exprs = new() {
									lhs,
									new CastExpr {
										op   = Operand.StaticCast,
										type = enumTypespec,
										expr = new BinOp {
											op = tuple.Item2,
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
								srcPos = srcPos,
								expr   = lhs,
							},
						}.ToBlock(),
					};

					if( isInline )
						ret.AssignAttribs( new() { { "inline", new() } } );

					ScopeLeaf scopeLeaf = new() {
						parent = namespaceUp,
						decl   = ret,
					};
					// can not directly put these in an enumeration or structural (maybe as friend?)
					namespaceUp.AddChild( scopeLeaf );
				}

			}
		}

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddHierarchical( this );
		}
	}

	public class Namespace : Hierarchical
	{
		public bool withBody;

		// TODO: what is needed here?
		public override void AddToGen( HierarchicalGen gen )
		{
			// can not be in a non-public context
			gen.AddHierarchical( this );
		}
	}

	public class GlobalNamespace : Namespace
	{
		public string          module;
		public HashSet<string> imps;
	}

	public class Structural : Hierarchical, ITplParams
	{
		public enum Kind
		{
			Struct,
			Class,
			Union,
		}

		public Kind                 kind;
		public List<TplParam>       TplParams { get; set; }
		public List<TypespecNested> basetypes;
		public List<TypespecNested> reqs;

		// default access for child elements
		public override Access defaultAccess
			=> kind == Kind.Class
				? Access.Private
				: Access.Public;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddHierarchical( this );
		}
	}
}
