using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Myll.Generator;

namespace Myll.Core
{
	using static String;

	using Strings = List<string>;
	using Attribs = Dictionary<string, List<string>>;

	public enum Access
	{
		None,
		Private,
		Protected,
		Public,
		Irrelevant,
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
		public Access    access;
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

		public bool IsInline {
			get {
				return IsAttrib( "inline" ) || IsTemplateUp;
				// HACK do not support parameters for the moment
				if( attribs != null && attribs.TryGetValue( "inline", out Strings val ) ) {
					int count = val.Count;
					return count switch {
						0 => true,
						1 => val[0].In( "force", "yes", "preferred", "maybe" ), // TODO: centralize these attribute-keywords
						_ => throw new NotSupportedException( "[inline(...)] with more than one parameter" ),
					};
				}
				else {
					return IsTemplateUp;
				}
			}
		}

		public bool IsInStruct => scope.parent.decl is Structural;

		// TODO Symbol?

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach( var info in GetType().GetProperties() ) {
				object value = info.GetValue( this, null )
				            ?? "(null)";
				sb.Append( info.Name + ": " + value.ToString() + ", " );
			}

			sb.Length = Math.Max( sb.Length - 2, 0 );
			return "{"
			     + GetType().Name + " '"
			     + name           + "' "
			     + sb.ToString()  + "}";
		}

		public string FullyQualifiedName {
			get {
				Strings ret = new Strings();
				for( ScopeLeaf cur = scope; cur?.parent != null; cur = cur.parent ) {
					Decl decl = cur.decl;
					string       name = decl?.name ?? "unknown_fix_me";
					if( decl is ITplParams curStruct )
						if( curStruct.TplParams.Count >= 1 )
							name += "<" + curStruct.TplParams
								.Select( t => t.name )
								.Join( ", " ) + ">";
					ret.Add( name );
				}
				// WTF dot net framework?
				return (ret as IEnumerable<string>).Reverse().Join( "::" );
			}
		}

		public abstract void AddToGen( HierarchicalGen gen );
	}

	// has in-order list of decls, visible from outside
	public abstract class Hierarchical : Decl
	{
		public readonly List<Decl> children = new List<Decl>();

		public new Scope scope {
			get => base.scope as Scope;
			set => base.scope = value;
		}

		public virtual Access defaultAccess { get => Access.Public; }

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

		// TODO: analyze, for void or auto return type of funcs
		public bool IsReturningSomething => false;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddFunc( this, access );
		}
	}

	// Constructor / Destructor
	public class ConDestructor : Decl
	{
		public enum Kind
		{
			Constructor,
			Destructor,
		}

		public Kind        kind;
		public List<Param> paras;
		public Stmt        block;

		// TODO: initlist

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddCtorDtor( this, access );
		}
	}

	public class Using : Decl
	{
		public List<TypespecNested> types;

		public override void AddToGen( HierarchicalGen gen )
			=> throw new NotImplementedException();
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
		public class Accessor
		{
			public enum Kind
			{
				Get,
				RefGet,
				Set,
			}

			public Kind      kind;
			public Qualifier qual;
			public Stmt      body; // opt
		}

		public Typespec       type;     // contains Qualifier
		public List<Accessor> accessor; // opt, structural or global
		public Expr           init;     // opt

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddVar( this, access );
		}
	}

	public class VarsDecl : Decl
	{
		public List<Var> vars;

		public override void AssignAttribs( Attribs attribs )
		{
			vars.ForEach( v => v.AssignAttribs( attribs ) );
		}

		public override void AddToGen( HierarchicalGen gen )
		{
			vars.ForEach( v => v.AddToGen( gen ) );
		}

		public override Strings Gen( int level )
		{
			return vars.SelectMany( v => v.Gen( level ) ).ToList();
		}
	}

	public class EnumEntry : Decl
	{
		public Expr value;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddEntry( this, Access.Public );
		}
	}

	public class Enumeration : Hierarchical
	{
		public TypespecBasic basetype;

		public bool IsFlags => IsAttrib( "flags" );

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
		bool IsPowerOfTwo(uint x)
		{
			bool ret = (x & (x - 1)) == 0;
			return ret;
		}

		// TODO this needs to be in the Generator Folder
		protected override void AttribsAssigned()
		{
			bool isFlags = IsAttrib( "flags" );
			if( isFlags ) {
				// HACK this is just written in a hurry, that might break at any user intervention
				uint index = 1;
				foreach( Decl child in children ) {
					if(child is EnumEntry ee ) {
						if( ee.value is Literal lit ) {
							uint read_index = uint.Parse( lit.text );
							if( read_index == 0 )
								index = 1;
							else
								index = read_index * 2;
						}
						else {
							if( !IsPowerOfTwo( index ) )
								throw new Exception(
									Format( "[flags]enum auto numbering not a power of two: {0} at {1}", index, ee.srcPos) );
							ee.value =  new Literal { op = Operand.Literal, text = index.ToString() };
							index    *= 2;
						}
					}
				}
			}

			bool isOpBitwise = IsAttrib( "operators", "bitwise" );
			if( isOpBitwise ) {
				bool  isInline    = IsInline;
				Scope namespaceUp = scope.UpToNamespace;

				// HACK: this should work for most, but is an incorrect Typespec
				Typespec
					enum_typespec = new TypespecNested {
						ptrs = new List<Pointer>(),
						idTpls = new List<IdTpl> {
							new IdTpl { id = FullyQualifiedName, tplArgs = new List<TemplateArg>() }
						}
					},
					enum_typespec_ref = new TypespecNested {
						ptrs = new List<Pointer> { new Pointer { kind = Pointer.Kind.LVRef } },
						idTpls = new List<IdTpl> {
							new IdTpl { id = FullyQualifiedName, tplArgs = new List<TemplateArg>() }
						}
					};

				Typespec underlying = new TypespecNested {
					ptrs = new List<Pointer>(),
					idTpls = new List<IdTpl> {
						new IdTpl { id = "std", tplArgs = new List<TemplateArg>() },
						new IdTpl {
							id      = "underlying_type",
							tplArgs = new List<TemplateArg> { new TemplateArg { typespec = enum_typespec } }
						},
						new IdTpl { id = "type", tplArgs = new List<TemplateArg>() },
					}
				};

				Expr
					lhs = new IdExpr
						{ op = Operand.Id, idTpl = new IdTpl { id = "lhs", tplArgs = new List<TemplateArg>() }, },
					rhs = new IdExpr
						{ op = Operand.Id, idTpl = new IdTpl { id = "rhs", tplArgs = new List<TemplateArg>() }, };

				foreach( (string, Operand) tuple in BitwiseOps ) {
					Func ret = new Func {
						srcPos    = srcPos, // TODO should be the pos of the attribute
						name      = tuple.Item1,
						access    = Access.Public,
						TplParams = new List<TplParam>(),
						retType   = enum_typespec,
						paras = new List<Param> {
							new Param { name = "lhs", type = enum_typespec },
							new Param { name = "rhs", type = enum_typespec },
						},
						block = new ReturnStmt {
							srcPos = srcPos,
							expr = new CastExpr {
								op   = Operand.StaticCast,
								type = enum_typespec,
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

					ScopeLeaf scopeLeaf = new ScopeLeaf {
						parent = namespaceUp,
						decl   = ret,
					};
					// can not directly put these in an enumeration or structural (maybe as friend?)
					namespaceUp.AddChild( scopeLeaf );
				}

				foreach( (string, Operand) tuple in BitwiseEqualOps ) {
					Func ret = new Func {
						srcPos    = srcPos, // TODO should be the pos of the attribute
						name      = tuple.Item1,
						access    = Access.Public,
						TplParams = new List<TplParam>(),
						retType   = enum_typespec_ref,
						paras = new List<Param> {
							new Param { name = "lhs", type = enum_typespec_ref },
							new Param { name = "rhs", type = enum_typespec },
						},
						block = new Block {
							srcPos = srcPos,
							stmts = new List<Stmt> {
								new MultiAssignStmt {
									srcPos = srcPos,
									exprs = new List<Expr> {
										lhs,
										new CastExpr {
											op   = Operand.StaticCast,
											type = enum_typespec,
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
									expr = lhs,
								},
							},
						},
					};

					if( isInline )
						ret.AssignAttribs( new Attribs { { "inline", new Strings() } } );

					ScopeLeaf scopeLeaf = new ScopeLeaf {
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
			gen.AddHierarchical( this, access );
		}
	}

	public class Namespace : Hierarchical
	{
		public bool withBody;

		// TODO: what is needed here?
		public override void AddToGen( HierarchicalGen gen )
		{
			// can not be in a non-public context
			gen.AddHierarchical( this, Access.Public );
		}
	}

	public class GlobalNamespace : Namespace
	{
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
			gen.AddHierarchical( this, access );
		}
	}
}
