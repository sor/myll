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
		public string    name;
		public Access    access = Access.Public;
		public ScopeLeaf scope;

		// recursive
		public bool IsTemplateUp {
			get {
				for( ScopeLeaf cur = scope; cur?.parent != null; cur = cur.parent )
					if( cur.decl is ITplParams curStruct )
						if( curStruct.TplParams.Count >= 1 )
							return true;

				return false;
			}
		}

		public bool IsInline   => IsAttrib( "inline" ) || IsTemplateUp;
		public bool IsInStruct => scope.parent.decl is Structural;

		// TODO Symbol?

		public override string ToString()
		{
			var sb = new StringBuilder();
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
				Strings ret = new Strings();
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
	}

	// has in-order list of decls, visible from outside
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
		public List<TplParam> TplParams { get; set; }
		public List<Param>    paras;
		public Stmt           block;
		public Typespec       retType;

		public bool IsVirtual  => IsAttrib( "virtual" );
		public bool IsConst    => IsAttrib( "const" ) || IsAttrib( "pure" );
		public bool IsOverride => IsAttrib( "override" );

		// TODO: analyze, for void or auto return type of funcs
		public bool IsReturningSomething => false;

		public override void AddToGen( HierarchicalGen gen )
		{
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
		public Stmt        block;

		public bool IsVirtual  => IsAttrib( "virtual" );
		public bool IsImplicit => IsAttrib( "implicit" );

		// TODO: initlist

		public override void AddToGen( HierarchicalGen gen )
		{
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
	public class Var : Decl
	{
		public Typespec       type;     // contains Qualifier
		public List<Accessor> accessor; // opt, structural or global
		public Expr           init;     // opt

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddVar( this );
		}
	}

	public class MultiDecl : Decl
	{
		public List<Decl> decls = new();

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
		public Expr value;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddEntry( this );
		}
	}

	public class Enumeration : Hierarchical
	{
		public TypespecBasic basetype;

		public bool IsFlags     => IsAttrib( "flags" );
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

		// 0 will return true as well, which is maybe not what you wanted
		bool IsPowerOfTwo( uint x )
		{
			bool ret = (x & (x - 1)) == 0;
			return ret;
		}

		// TODO this needs to be in the Generator Folder
		protected override void AttribsAssigned()
		{
			bool isFlags = IsFlags;
			if( isFlags ) {
				// HACK this is just written in a hurry, that might break at any user intervention
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

			bool isOpBitwise = IsOpBitwise;
			if( isOpBitwise ) {
				bool  isInline    = IsInline;
				Scope namespaceUp = scope.UpToNamespace;

				// HACK: this should work for most, but is an incorrect Typespec
				Typespec
					enumTypespec = new TypespecNested {
						idTpls = new List<IdTplArgs> {
							new() { id = FullyQualifiedName, tplArgs = new List<TplArg>() }
						}
					},
					enumTypespecRef = new TypespecNested {
						ptrs   = new List<Pointer> { new() { kind = Pointer.Kind.LVRef } },
						idTpls = new List<IdTplArgs> { new() { id = FullyQualifiedName, tplArgs = new List<TplArg>() } }
					};

				Typespec underlying = new TypespecNested {
					idTpls = new List<IdTplArgs> {
						new() { id = "std", tplArgs = new List<TplArg>() },
						new() {
							id      = "underlying_type",
							tplArgs = new List<TplArg> { new() { typespec = enumTypespec } }
						},
						new() { id = "type", tplArgs = new List<TplArg>() },
					}
				};

				Expr
					lhs = new IdExpr
						{ op = Operand.Id, idTplArgs = new IdTplArgs { id = "lhs", tplArgs = new List<TplArg>() }, },
					rhs = new IdExpr
						{ op = Operand.Id, idTplArgs = new IdTplArgs { id = "rhs", tplArgs = new List<TplArg>() }, };

				foreach( (string, Operand) tuple in BitwiseOps ) {
					Func ret = new() {
						srcPos    = srcPos, // TODO should be the pos of the attribute
						name      = tuple.Item1,
						TplParams = new List<TplParam>(),
						retType   = enumTypespec,
						paras = new List<Param> {
							new() { name = "lhs", type = enumTypespec },
							new() { name = "rhs", type = enumTypespec },
						},
						block = new ReturnStmt {
							srcPos = srcPos,
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
						},
					};

					ret.AssignAttribs(
						isInline
							? new Attribs { { "ct", new Strings() }, { "inline", new Strings() } }
							: new Attribs { { "ct", new Strings() } } );

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
						TplParams = new List<TplParam>(),
						retType   = enumTypespecRef,
						paras = new List<Param> {
							new() { name = "lhs", type = enumTypespecRef },
							new() { name = "rhs", type = enumTypespec },
						},
						block = new Block {
							srcPos = srcPos,
							stmts = new List<Stmt> {
								new MultiAssign {
									srcPos = srcPos,
									exprs = new List<Expr> {
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
							},
						},
					};

					if( isInline )
						ret.AssignAttribs( new Attribs { { "inline", new Strings() } } );

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
