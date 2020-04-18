using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static System.String;

using static Myll.Generator.StmtFormatting;

namespace Myll.Core
{
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
	public class Decl : Stmt
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
	}

	// has in-order list of decls, visible from outside
	public class Hierarchical : Decl
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

	public class Func : Decl, ITplParams
	{
		public class Param
		{
			public Typespec type;
			public string   name;

			public string Gen()
			{
				return type.Gen( name );
			}
		}

		// fac(n: 1+2) // n is matching _name_ of param, 1+2 is _expr_
		public class Arg // Decl
		{
			public string name; // opt
			public Expr   expr;

			public string Gen()
			{
				if( !IsNullOrEmpty( name ) )
					throw new NotImplementedException( "named function arguments needs to be implemented" );

				return expr.Gen();
			}
		}

		public class Call
		{
			public List<Arg> args;
			public bool      indexer;
			public bool      nullCoal;

			public string Gen()
			{
				if( nullCoal )
					throw new NotImplementedException( "null coalescing for function calls needs to be implemented" );

				if( indexer ) {
					// TODO: call a different method that can handle more than one parameter
					if( args.Count != 1 )
						throw new TargetParameterCountException("indexer call with != 1 arguments");

					return "[" + args.Select( a => a.Gen() ).Join( ", " ) + "]";
				}
				else {
					if( args.Count == 0 )
						return "()";

					return "( " + args.Select( a => a.Gen() ).Join( ", " ) + " )";
				}
			}
		}

		public List<TplParam> TplParams { get; set; }
		public List<Param>    paras;
		public Stmt           block;
		public Typespec       retType;

		public bool IsReturningSomething => false; // TODO: analyze, for void or auto return type of funcs

		public override void AddToGen( DeclGen gen )
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

		public Kind             kind;
		public List<Func.Param> paras;
		public Stmt             block;

		// TODO: initlist

		public override void AddToGen( DeclGen gen )
		{
			gen.AddCtorDtor( this, access );
		}
	}

	public class Using : Decl
	{
		public List<TypespecNested> types;
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

		public override void AddToGen( DeclGen gen )
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

		public override void AddToGen( DeclGen gen )
		{
			vars.ForEach( v => v.AddToGen( gen ) );
		}

		public override Strings Gen( int level )
		{
			return vars.SelectMany( v => v.Gen( level ) ).ToList();
		}
	}

	public class Enum : Hierarchical
	{
		public class Entry : Decl
		{
			public Expr value;

			public override void AddToGen( DeclGen gen )
			{
				gen.AddEntry( this );
			}
		}

		public TypespecBasic basetype;
		public bool flags; // TODO: most likely obsolete now

		public override void AddToGen( DeclGen gen )
		{
			gen.AddHierarchical( this );
		}
	}

	public class Namespace : Hierarchical
	{
		public bool withBody;

		// TODO: what is needed here?
		public override void AddToGen( DeclGen gen )
		{
			gen.AddNamespace( this );
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

		//public Access currentAccess;

		public override Access defaultAccess
			=> kind == Kind.Class
				? Access.Private
				: Access.Public;

		public override void AddToGen( DeclGen gen )
		{
			gen.AddHierarchical( this, access );
		}
	}
}
