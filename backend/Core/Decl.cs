using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Myll;
using Myll.Generator;

namespace Myll.Core
{
	using static String;

	using Attribs = Dictionary<string, List<string>>;

	public enum Access
	{
		Private,
		Protected,
		Public,
	}

	interface ITemplatedDecl
	{
		List<TplParam> TplParams { get; }
		Scope?         TemplateParamScope { get; }
	}

	/// <summary>
	/// introduces a name (most of the time)
	/// </summary>
	public abstract class Decl : AttributedNode
	{
		public string    name { get; init; } = null!;
		public Access    access = Access.Public;
		public ScopeLeaf scope = null!;

		/// <summary>
		/// Resolved specialization data for a template specialization (variable or
		/// class/struct/union), filled by the pre-resolution transformer.
		/// </summary>
		public sealed class SpecData
		{
			public Decl         Primary = null!;
			public List<TplArg> Args  = new(); // all primary params, declaration order
			public bool[]       Bound = System.Array.Empty<bool>();
			public bool IsFull => Bound.Length > 0 && Bound.All( b => b );

			// A [spec=some] block head: the declaration itself is not a
			// specialization of the primary; it only collects member
			// specializations for the concrete instantiation named by Args.
			// Such heads stay transparent to name lookup and emit no class.
			public bool MemberSpecHead { get; set; }
		}

		/// <summary>
		/// Filled for template specializations: the primary plus the bound template
		/// arguments. Args lists every primary parameter in declaration order; unbound
		/// positions carry a symbolic TypespecNested naming the parameter, and Bound
		/// marks the bound positions.
		/// </summary>
		public SpecData? SpecInfo { get; set; }

// recursively check if there is anything templated surrounding
		public bool IsTemplateUp {
			get {
			for( ScopeLeaf cur = scope; cur?.parent != null; cur = cur.parent ) {
				if( cur.decl.SpecInfo != null )
					return true; // inside a specialization: header entity like a template
				if( cur.decl is ITemplatedDecl curStruct )
					if( curStruct.TplParams.Count >= 1 )
						return true;
			}

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
			SourceLines ret = new();
			for( ScopeLeaf cur = scope; cur?.parent != null; cur = cur.parent ) {
				Decl?  decl     = cur.decl;
				string declName = decl?.name ?? "unknown_fix_me";
				bool includeTplParams = includeOwnTemplateParams || cur != scope;
				if( includeTplParams && decl != null && decl.SpecInfo != null ) {
					// A specialization names its primary's arguments, not its own
					// (unbound) parameters: Box<int>::member.
					declName += "<" + decl.SpecInfo.Args
						.Select( a => a.Gen() )
						.Join( ", " ) + ">";
				}
				else if( includeTplParams
				 && decl is ITemplatedDecl curStruct
				 && curStruct.TplParams.Count >= 1 )
					declName += "<" + curStruct.TplParams
						.Select( t => t.name )
						.Join( ", " ) + ">";
				ret.Add( declName );
			}
			// WTF dot net framework?
			return string.Join( "::", ret.AsEnumerable().Reverse().Select( l => l.Text ) );
		}

		public abstract void AddToGen( HierarchicalGen gen );

		public override void AssignAttribs( Attribs inAttribs )
		{
			// Extract from the node's own copy, never from the caller's dictionary.
			base.AssignAttribs( inAttribs );
			ExtractAccessAttribs( attribs );
		}

		private void ExtractAccessAttribs( Attribs inAttribs )
		{
			if( inAttribs == null )
				return;

			if( inAttribs.TryGetValue( "access", out List<string>? values ) && values.Count > 0 ) {
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
	public class Func : Decl, ITemplatedDecl, IBodyOwner
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
		public List<TplParam>       TplParams { get; set; } = new();
		public List<TplBinding>     TplSpec { get; init; } = new(); // non-empty: full specialization of a primary (same module or imported)
		public List<TypespecNested> Requires  { get; init; } = new(); // TODO: replace with dedicated type
		public List<Param>          paras   = new();
		public MultiStmt?           body; // isScope = true
		public Typespec             retType = null!;

		/// <summary>
		/// The scope that holds parameters and any compiler-generated locals for this
		/// function. It is a child of the enclosing class/struct/global scope.
		/// </summary>
		public Scope? funcScope;

		public Scope? TemplateParamScope => funcScope;

		/// <summary>
		/// Namespace names that must be emitted as <c>using namespace NS;</c> at the start
		/// of this function's body because the function is inside a scope with a contained
		/// namespace using (module-scope in Contained mode, or class/struct scope).
		/// </summary>
		public List<string> InjectedNamespaceUsings { get; } = new();

		public override void AddToGen( HierarchicalGen gen ) { gen.AddFunc( this ); }
	}

	// Constructor / Destructor
	public class Structor : Decl, ITemplatedDecl, IBodyOwner
	{
		public enum Kind
		{
			Constructor,
			Destructor,
		}

		public Kind        kind;
		public bool        isConversionCtor; // true for `convert <- T` declarations
		public List<Param> paras = new();
		public MultiStmt?  body;

		public List<TplParam>   TplParams { get; set; } = new();
		public List<TplBinding> TplSpec   { get; init; } = new(); // non-empty: specialization of a primary (same module or imported)
		public Scope?           ctorScope;

		public Scope? TemplateParamScope => ctorScope;

		/// <summary>
		/// See <see cref="Func.InjectedNamespaceUsings"/>.
		/// </summary>
		public List<string> InjectedNamespaceUsings { get; } = new(); // isScope = true

		// TODO: initlist

		public override void AddToGen( HierarchicalGen gen ) { gen.AddStructor( this ); }
	}


	/// <summary>
	/// Implemented by declarations that own a function-like body and therefore may need
	/// a <c>using namespace</c> directive injected at the start of that body for
	/// contained namespace usings.
	/// </summary>
	public interface IBodyOwner
	{
		List<string> InjectedNamespaceUsings { get; }
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

	public class AliasDecl : Decl, ITemplatedDecl
	{
		public Typespec       type     = null!;
		public List<TplParam> TplParams { get; init; } = new();
		public List<TplBinding> TplSpec { get; init; } = new(); // non-empty: rejected — alias templates cannot be specialized
		public Scope?         aliasScope;
		public Scope?         TemplateParamScope => aliasScope;

		// Set by the resolver when the aliased type is a namespace.
		public bool IsNamespaceAlias { get; set; }

		/// <summary>
		/// A bound-parameter alias of a template specialization: injected instead
		/// of substitution so that specializations can keep using the parameter
		/// name in signatures and bodies. Uses emit the aliased type, not the name.
		/// </summary>
		public bool IsBoundAlias { get; set; }

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
	public class VarDecl : Decl, ITemplatedDecl
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
		public bool           isDirectConstruct;
		public List<TplParam> TplParams { get; set; } = new();
		public List<TplBinding> TplSpec { get; init; } = new(); // non-empty: specialization of a primary (same module or imported)
		public Scope?         varScope;

		public Scope? TemplateParamScope => varScope;

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
		public TplParamKind kind = TplParamKind.Type;
		public Typespec?    type;

		// The parameter was declared `<T...>`: a pack parameter. Types that
		// resolve to this decl are pack-typed.
		public bool isPack;

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
		public string          module  = null!;
		public List<ImportSpec> imports = new();
	}

	public class BaseType
	{
		public Access         access    = Access.Public;
		public bool           isVirtual = false;
		public TypespecNested type      = null!;
	}

	public class Structural : Hierarchical, ITemplatedDecl
	{
		public enum Kind
		{
			Struct,
			Class,
			Union,
		}

public Kind                 kind;
		public List<TplParam>       TplParams { get; set; } = new();
		public List<TplBinding>     TplSpec { get; init; } = new(); // non-empty: specialization of a primary (same module or imported)
		public List<BaseType>       basetypes = new();
		public List<TypespecNested> reqs = new();

		public Scope? TemplateParamScope => scope;

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
