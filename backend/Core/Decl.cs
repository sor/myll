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
		public bool IsTemplate {
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
				if( attribs != null && attribs.TryGetValue( "inline", out Strings val ) ) {
					int count = val.Count;
					return count switch {
						0 => true,
						1 => val[0].In( "force", "yes", "preferred", "maybe" ), // TODO: centralize these attribute-keywords
						_ => throw new NotSupportedException( "[inline(...)] with more than one parameter" ),
					};
				}
				else {
					return IsTemplate;
				}
			}
		}

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

		public bool IsFlags => attribs?.ContainsKey( "flags" ) ?? false;

		protected virtual void AttribsAssigned()
		{
			if( attribs == null )
				return;

			if( attribs.ContainsKey( "operators" ) ) {
				var par = scope.parent;

				// HACK: this should work for most, but is an incorrect Typespec
				Typespec enum_typespec = new TypespecNested {
					idTpls = new List<IdTpl> {
						new IdTpl{ id = FullyQualifiedName }
					}
				};
				Func ret = new Func {
					srcPos    = srcPos,
					name      = "operator|",
					access    = Access.Public,
					TplParams = new List<TplParam>(),
					paras = new List<Param> {
						new Param { name = "left", type  = enum_typespec },
						new Param { name = "right", type = enum_typespec },
					},
					block   = new Block{},
					retType = enum_typespec,
				};

				//hier muss weiter gemacht werden, adde den operator zum global scope
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
