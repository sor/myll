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
	/// </summary>
	public abstract class Decl : AttributedNode
	{
		public string    name { get; init; } = null!;
		public Access    access = Access.Public;
		public ScopeLeaf scope = null!;

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

		public bool IsForwardDeclaration { get; set; }
		public bool IsExternNamespace    { get; set; }
		public bool IsExternal           => IsExternNamespace || IsExtern;
		public bool IsInlined            => IsInline || IsTemplateUp;
		public bool IsInStruct           => scope?.parent?.decl is Structural;

		/// <summary>
		/// True for symbols that live in a block/function scope and are not emitted as
		/// top-level or field declarations: parameters, local variables, catch variables,
		/// <c>times</c> indices, and lambda parameters.
		/// </summary>
		public bool IsLocal { get; set; }

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

		public string FullyQualifiedName
			=> BuildQualifiedName( includeOwnTemplateParams: true );

		/// <summary>
		/// Like <see cref="FullyQualifiedName"/> but omits the declaring entity's own
		/// template parameter list. Used when the caller supplies explicit template
		/// arguments (e.g. <c>Ns::foo&lt;int&gt;</c>).
		/// </summary>
		public string ReferenceName
			=> BuildQualifiedName( includeOwnTemplateParams: false );

		private string BuildQualifiedName( bool includeOwnTemplateParams )
		{
			Strings ret = new();
			for( ScopeLeaf cur = scope; cur?.parent != null; cur = cur.parent ) {
				Decl?  decl     = cur.decl;
				string declName = decl?.name ?? "unknown_fix_me";
				bool includeTplParams = includeOwnTemplateParams || cur != scope;
				if( includeTplParams
				 && decl is ITplParams curStruct
				 && curStruct.TplParams.Count >= 1 )
					declName += "<" + curStruct.TplParams
						.Select( t => t.name )
						.Join( ", " ) + ">";
				ret.Add( declName );
			}
			// WTF dot net framework?
			return ((IEnumerable<string>) ret).Reverse().Join( "::" );
		}

		public abstract void AddToGen( HierarchicalGen gen );

		public override void AssignAttribs( Attribs inAttribs )
		{
			ExtractAccessAttribs( inAttribs );
			base.AssignAttribs( inAttribs );
		}

		private void ExtractAccessAttribs( Attribs inAttribs )
		{
			if( inAttribs == null )
				return;

			if( inAttribs.TryGetValue( "access", out Strings? values ) && values.Count > 0 ) {
				access = values[0] switch {
					"pub"  => Access.Public,
					"priv" => Access.Private,
					"prot" => Access.Protected,
					_      => throw new NotSupportedException(
						"unknown access value: " + values[0] ),
				};
				inAttribs.Remove( "access" );
				return;
			}

			if( inAttribs.ContainsKey( "pub" ) ) {
				access = Access.Public;
				inAttribs.Remove( "pub" );
			}
			else if( inAttribs.ContainsKey( "priv" ) ) {
				access = Access.Private;
				inAttribs.Remove( "priv" );
			}
			else if( inAttribs.ContainsKey( "prot" ) ) {
				access = Access.Protected;
				inAttribs.Remove( "prot" );
			}
		}
	}

	// Has an in-order list of decls, visible from outside
	public abstract class Hierarchical : Decl
	{
		public readonly List<Decl> children = new();

		public new Scope scope {
			get => (base.scope as Scope)!;
			set => base.scope = value;
		}

		public virtual Access defaultAccess => Access.Public;

		// the children add themselves through AddChild or PushScope
		public void AddChild( Decl decl ) { children.Add( decl ); }
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
		public List<Param>          paras   = new();
		public MultiStmt?           body; // isScope = true
		public Typespec             retType = null!;

		/// <summary>
		/// The scope that holds parameters and any compiler-generated locals for this
		/// function. It is a child of the enclosing class/struct/global scope.
		/// </summary>
		public Scope? funcScope;

		// TODO: analyze, for void or auto return type of funcs
		public bool IsReturningSomething => false;

		public override void AddToGen( HierarchicalGen gen ) { gen.AddFunc( this ); }
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
		public List<Param> paras = new();
		public MultiStmt?  body; // isScope = true

		// TODO: initlist

		public override void AddToGen( HierarchicalGen gen ) { gen.AddStructor( this ); }
	}

	public class UsingDecl : Decl
	{
		// in locations where C++ does not support "using (namespace)" this must not be printed
		// but instead the unqualified types need to be changed to qualified ones
		public Typespec type = null!;

		// Set by the resolver when this using declaration targets a namespace.
		public bool IsNamespaceUsing { get; set; }

		public override void AddToGen( HierarchicalGen gen ) { gen.AddUsing( this ); }
	}

	public class AliasDecl : Decl
	{
		public Typespec type = null!;

		// Set by the resolver when the aliased type is a namespace.
		public bool IsNamespaceAlias { get; set; }

		public override void AddToGen( HierarchicalGen gen ) { gen.AddAlias( this ); }
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
		public Typespec       type     = null!; // contains Qualifier
		public List<Accessor> accessor = new(); // opt, structural or global
		public Expr?          init;

		public override void AddToGen( HierarchicalGen gen )
		{
			gen.AddVar( this );
		}
	}

	/// <summary>
	/// Compiler-internal declaration for a type template parameter (e.g. the T in
	/// <c>class C&lt;T&gt;</c> or <c>func f&lt;T&gt;()</c>). It only exists in scope so that
	/// the resolver accepts uses of the parameter name; it is never emitted.
	/// </summary>
	public class TplParamDecl : Decl
	{
		public TplParamDecl()
		{
			IsLocal = true;
			AssignAttribs( new Attribs { ["hide"] = new List<string>() } );
		}

		public override void AddToGen( HierarchicalGen gen )
		{
			throw new InvalidOperationException(
				"TplParamDecl should never reach code generation." );
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
		public TypespecBasic? baseType;

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
		public string          module = null!;
		public HashSet<string> imps = new();
	}

	public class BaseType
	{
		public Access         access    = Access.Public;
		public bool           isVirtual = false;
		public TypespecNested type      = null!;
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
		public List<TplParam>       TplParams { get; set; } = new();
		public List<BaseType>       basetypes = new();
		public List<TypespecNested> reqs = new();

		// default access for child elements
		public override Access defaultAccess
			=> kind == Kind.Class
				? Access.Private
				: Access.Public;

		public override void AddToGen( HierarchicalGen gen )
		{
			if( IsForwardDeclaration || IsExternal ) {
				gen.AddForwardDecl( this );
				return;
			}

			gen.AddHierarchical( this );
		}
	}
}
