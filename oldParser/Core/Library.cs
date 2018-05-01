using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using MyLang.Core;

/**
 * die pointer brauchen nur hier hin zu zeigen, die library hat alle infos die es selbst braucht
 * sollte jemand von ausserhalb mehr infos brauchen fragt er die library
 * 
 * die library ist die deklaration, die fleischlose kopie der implementierung
 * quasi die headerfiles... die ich hiermit abzulösen versuche :ugly:
 **/

// offline-able hierarchy of all named stuff

// ::std namespace
//   ::vec class<T>
//     .data  -   var(T*)
//     .count +-- prop(int)
//     .push  +   func(T&)
//     .top   +   func() -> T&
//   ::dict class<K,V> : vec<pair<K,V>>
//   ::math namespace
//     ::sqrt func(double) -> double
namespace MyLang.Library {

	// { name: "list", parent: Fragment(NS JanSordid::Core), children: [Fragment(Meth test)],
	//   using_namespaces: [], bases: [], kind: Class }
	// This is the new Shit
	public class Fragment {
		public enum Kind {
			Undefined,
			BasicType,
			AdvancedType,	// obsolete? this is stuct, class, union, enum, maybe namespace
			Namespace,		// Structural ff
			Struct,
			Class,
			Union,
			Enum,
			GlobalVar,		// Field ff
			InternalVar,	// anon-ns / non-class-static
			Field,
			Property,
			Function,
			Method,
			// Non-persistent
			FunctionCall,
			MethodCall,
			UnaddressableScope
		}
		
		const int UNKNOWN = -1;

		protected	int					unresolved	= UNKNOWN;
		public		Fragment			resolved	= null;
		public		string 				name		= "";
		public		bool				is_member	= false;
		public		Kind				kind		= Kind.Undefined;
		public		Fragment			parent		= null;
		public		Fragment			type		= null;
		public		HashSet<Fragment>	using_namespaces	= new HashSet<Fragment>();
		public		List<Fragment>		children			= new List<Fragment>();
		public		List<Fragment>		base_classes		= new List<Fragment>();
		public		List<Fragment>		parameters			= new List<Fragment>();
		public		List<List<Fragment>> templates_parameters = new List<List<Fragment>>();

		//Dictionary<string,Named>	children			= new Dictionary<string, Named>();
		//HashSet<Hierarchic>			using_namespaces	= new HashSet<Hierarchic>();

		//	protected Flags				flags;

		//	public string FullName { get { return parent == null ? "NONAME???" : parent.FullName; } }

		public bool IsResolved { get { return unresolved == 0; } }
		public string FullName { get { return parent == null ? name : parent.FullName + "::" + name; } }

		public Fragment( string name ) {
			this.name = name;
		}

		// returns and stores number of unresolved ones
		// calls Resolve on all children
		public virtual int Resolve() {
			if( IsResolved )
				return 0;

			ResolveScope rs = new ResolveScope() {
				start_scope		 = this,
				using_namespaces = GenerateUsingNamespaces()
			};
			return unresolved = children.Sum( q => q.Resolve() );
		}

		public virtual void Unresolve() {
			unresolved = -1;
			if( parent != null && parent.IsResolved )
				parent.Unresolve();
		}

		public virtual HashSet<Fragment> GenerateUsingNamespaces() {
			HashSet<Fragment> ret = new HashSet<Fragment>( using_namespaces );

			foreach( var cur in TraverseUp() )
				ret.UnionWith( cur.using_namespaces );

			return ret;
		}

		// starts at parent
		IEnumerable<Fragment> TraverseUp() {
			for( var cur = parent; cur != null; cur = cur.parent )
				yield return cur;
		}

		public void AddFunction( string name, IEnumerable<Fragment> parameters, Fragment return_type ) {
			Fragment func = new Fragment( name )
			{
				kind		= Fragment.Kind.Function,
				parameters	= new List<Fragment>( parameters ),
				type		= return_type,
			};

			Add( func );
		}

		// Adds a child, sets its parent
		public void Add( Fragment child ) {
			//Debug.Assert(
			//	!children.ContainsKey( child.name ),
			//	"Duplicate child name: " + child + " in: " + this );

			children.Add( child );
			child.parent = this;
			child.Unresolve();
		}

		// search my children
		public Fragment Search( string name ) {
			return children.Find( q => q.name == name );
		}

		// search me and my ancestors
		public Fragment SearchUp( string name ) {
			var ret = Search( name );

			if( ret != default( Fragment ) )
				return ret;

			if( parent != default( Fragment ) )
				return parent.SearchUp( name );

			return default( Fragment );
		}

		// implicit conversion from string to Type
		public static implicit operator Fragment( string name ) {
			return new Fragment( name );
		}

		public override string ToString() {
			return base.ToString() + " " + FullName;
		}
	}

	//class ScopeElement {
	//	[Flags]
	//	public enum Flags {
	//		Invalid		= 0,
	//		Named		= 1 << 0,	// Does it have a name, unnamed ScopeElements will not be persisted
	//		Static		= 1 << 1,	// Is it static
	//		HasChildren	= 1 << 2,	// Does it contain child elements
	//		// What Kind is it
	//		TypeDecl	= 1 << 4,
	//		VarDecl		= 1 << 5,
	//		FuncDecl	= 1 << 6,
	//	}

	//	protected int				unresolved = -1;
	//	protected Flags				flags;
	//	protected ScopeContainer	parent;

	//	public bool IsResolved { get { return unresolved == 0; } }
	//	public string FullName { get { return parent == null ? "NONAME???" : parent.FullName; } }

	//	public ScopeElement( Flags flags, ScopeContainer parent ) {
	//		this.flags	= flags;
	//		this.parent	= parent;
	//	}

	//	// returns and stores number of unresolved ones
	//	public abstract int Resolve();

	//	public virtual void Unresolve() {
	//		unresolved = -1;
	//		if( parent != null && parent.IsResolved )
	//			parent.Unresolve();
	//	}

	//	public override string ToString() {
	//		return base.ToString() + " " + FullName;
	//	}

	//	class Class		: ScopeElement { Flags flags = Flags.Named | Flags.Static | Flags.HasChildren | Flags.TypeDecl; }
	//	class Enum		: ScopeElement { Flags flags = Flags.Named | Flags.Static | Flags.HasChildren | Flags.TypeDecl; }
	//	class Field		: ScopeElement { Flags flags = Flags.Named | Flags.VarDecl;	}
	//	class Variable	: ScopeElement { Flags flags = /*Flags.Named |*/ Flags.VarDecl;	}
	//}
	//class ScopeContainer : ScopeElement, IScopeContainer {
	//	List<ScopeElement>		children		 = new List<ScopeElement>();
	//	HashSet<ScopeElement>	using_namespaces = new HashSet<ScopeElement>();
	//	public ScopeContainer( Flags flags, ScopeContainer parent )
	//		: base( flags, parent )
	//	{}
	//	// calls Resolve on all children
	//	public override int Resolve() {
	//		if( IsResolved )
	//			return 0;

	//		ResolveScope rs = new ResolveScope() {
	//			start_scope		 = this,
	//			using_namespaces = GenerateUsingNamespaces()
	//		};

	//		return unresolved = children.Sum( q => q.Resolve() );
	//	}
	//}
	//class ScopeNamedElement : ScopeElement, IScopeNamed {
	//	protected string name;
	//	public ScopeNamedElement( Flags flags, ScopeContainer parent, string name )
	//		: base( flags, parent )
	//	{
	//		this.name = name;
	//	}
	//	public string FullName { get { return parent == null ? name : parent.FullName + "::" + name; } }
	//}
	//class ScopeNamedContainer : ScopeContainer, IScopeNamed, IScopeContainer {
	//	protected string name;
	//	public ScopeNamedContainer( Flags flags, ScopeContainer parent, string name )
	//		: base( flags, parent )
	//	{
	//		this.name = name;
	//	}
	//	public string FullName { get { return parent == null ? name : parent.FullName + "::" + name; } }
	//}
	//class ScopeVariable : ScopeNamedElement {
	//	Type type;
	//	public override int Resolve() {
	//		if( IsResolved )
	//			return 0;

	//		type.parent = parent;
	//		return unresolved = type.Resolve();
	//	}
	//}
	//// entweder textuell benannt (name von Basisklasse)
	//// oder ein Zeiger auf ein anderes Element (resolved)
	//class ScopedType : ScopeNamedElement {
	//	private ScopeElement resolved;

	//	public ScopedType( Flags flags, ScopeContainer parent, string name )
	//		: base( flags, parent, name ) { }

	//	// fills resolved_type_ptr by calling SearchUp on parent
	//	public override int Resolve() {
	//		resolved = parent.SearchUp( name );
	//		if( resolved != null )
	//			return 0;
	//		else
	//			return 1;
	//	}

	//	// implicit conversion from string to Type
	//	public static implicit operator Type( string name ) {
	//		return new Type( name );
	//	}
	//}
	
	public static class Extension {
		public static Named SearchNS( this IEnumerable<Hierarchic> using_namespaces, string name ) {
			var ns = using_namespaces.Select( q => q.Search( name ) ).OfType<Named>();
			switch( ns.Count() )
			{
				case 0:		throw new Exception( name + " not found in any of the available namespaces!" );
				case 1:		return ns.First();
				default:	throw new Exception( name + " too many hits found in any of the available namespaces!\n" + string.Join( ",", ns.Select( q => q.FullName ) ) );
			}
		}
	}

	interface IResolvable {
		bool	IsResolved { get; }

		int		Resolve();
		void	Unresolve();
	}

	class ResolveScope {
		public Fragment				start_scope;
		public HashSet<Fragment>	using_namespaces = new HashSet<Fragment>();
	}

	/// <summary>
	/// Anything that has a designator, a name.
	/// On creation its just a string, but later on it will be resolved to
	/// an instance of the correct 
	/// </summary>

	abstract public class Named {
		public string 		name;
		public Hierarchic	parent;
		protected int			unresolved = -1;

		public bool IsResolved { get { return unresolved == 0; } }
		public string FullName { get { return parent == null ? name : parent.FullName + "::" + name; } }

		public Named( string name ) {
			this.name = name;
		}

		// returns and stores number of unresolved ones
		public abstract int Resolve();

		public virtual void Unresolve() {
			unresolved = -1;
			if( parent.IsResolved )
				parent.Unresolve();
		}

		public override string ToString() {
			return base.ToString() + " " + FullName;
		}
	}

	// can be a leaf (no children)
	public interface IChild<out PT, out CT>
			where PT : class, IParent<PT,CT>
			where CT : class, IChild<PT,CT> {
		PT Parent { get; }
	}
	// is always a child to
	public interface IParent<out PT, out CT> : IChild<PT,CT>
			where PT : class, IParent<PT,CT>
			where CT : class, IChild<PT,CT>{
		IEnumerable<CT> Children { get; }
	}

	// Namespace if used directly
	public class Hierarchic : Named//, IParent<Hierarchic,Named>
	{
		Dictionary<string,Named>	children			= new Dictionary<string,Named>();
		HashSet<Hierarchic>			using_namespaces	= new HashSet<Hierarchic>();

		//IEnumerable<Named>	IParent<Hierarchic,Named>.Children	{ get; set; }
		//Hierarchic			IChild<Hierarchic, Named>.Parent	{ get; set; }

		public virtual HashSet<Hierarchic> GenerateUsingNamespaces() {
			HashSet<Hierarchic> ret = new HashSet<Hierarchic>( using_namespaces );

			foreach( var cur in TraverseUp() )
				ret.UnionWith( cur.using_namespaces );

			return ret;
		}

		// starts at parent
		IEnumerable<Hierarchic> TraverseUp() {
			for( var cur = parent; cur != null; cur = cur.parent )
				yield return cur;
		}

		public Hierarchic( string name )
			: base( name ) {
			this.name = name;
		}

		// Adds to children, sets the parent of the Function
		// Adds as an Overload if Function exists already
		public void AddFunction( string name, Overload overload ) {
			Named fn;
			Function ffn;
			if( children.TryGetValue( name, out fn ) )	// TODO: testcase
			{
				// add as overload
				ffn					= (Function)fn;
				var old_count		= ffn.overloads.Count;
				overload.function	= ffn;
				ffn.overloads.Add( overload );
				Debug.Assert(
					old_count != ffn.overloads.Count,
					"Identical overloads in function: " + name + "(" + overload.parameter_types.ToString() + ")" + " into: " + this );
			}
			else
			{
				// create new function, first overload
				ffn = new Function( name ) {
					overloads	= { overload },
					parent		= this
				};
				overload.function = ffn;
				children.Add( name, ffn );
			}
			ffn.Unresolve();
		}

		// Adds to children, sets the parent of the named
		public void Add( Named named ){
			Debug.Assert(
				!children.ContainsKey( named.name ),
				"Duplicate child name: " + named +  " in: " + this );

			named.parent = this;
			children.Add( named.name, named );
			named.Unresolve();
		}
		// search my children
		public Named Search( string name ) {
			Named value;
			children.TryGetValue( name, out value );
			return value;
		}
		// search me and my ancestors
		public Named SearchUp( string name ) {
			var ret = Search( name );
			
			if( ret != default( Named ) )
				return ret;

			if( parent != null )
				return parent.SearchUp( name );

			return null;
		}
		// calls Resolve on all children
		public override int Resolve()
		{
			if( IsResolved )
				return 0;

			//ResolveScope rs = new ResolveScope() { start_scope = this, using_namespaces = GenerateUsingNamespaces() };
			return unresolved = children.Values.Sum( q => q.Resolve() );
		}
	}

	// entweder textuell benannt (name von Basisklasse)
	// oder ein Zeiger auf ein anderes Element (resolved)
	public class Type : Named {
		private Named	resolved;

		public Type( string name )
			: base( name ) { }

		// fills resolved_type_ptr by calling SearchUp on parent
		public override int Resolve() {
			resolved = parent.SearchUp( name );
			if( resolved != null )	return 0;
			else					return 1;
		}

		// implicit conversion from string to Type
		public static implicit operator Type( string name ) {
			return new Type( name );
		}
	}

	public class Basic : Named {
		MyBasicType backend;

		public Basic( string name )
			: base( name ) {
			this.unresolved = 0;
		}

		public override int Resolve() { return 0; }
		public override void Unresolve() {}
	}

	public class Advanced : Named {
		MyAdvancedType backend;

		public Advanced( string name )
			: base( name ) {
		}

		public override int Resolve() {
			throw new NotImplementedException();
		}
	}

	public class Structural : Hierarchic {
		/* template, base classes, member functions */
		public enum Kind {
			Undefined,
			Namespace,
			Struct,
			Class,
			Union,
			Enum
		}

		public Kind kind;

		public Structural( string name, Kind kind )
			: base( name ) {
			this.kind = kind;
		}
	}

	public class Field : Named {
		public enum Kind {
			Undefined,
			Global,
			Internal,	// anon-ns / non-class-static
			Field,
			Property
		}

		public Kind				kind;
		public Type				type;
		public readonly bool	is_member = false;

		public Field( string name, Kind kind )
			: base( name ) {
			this.kind = kind;
		}

		public override int Resolve() {
			if( IsResolved )
				return 0;

			type.parent = parent;
			return unresolved = type.Resolve();
		}
	}

	// names can clash, multiple overloads are in overloads
	public class Function : Hierarchic {
		public HashSet<Overload>	overloads = new HashSet<Overload>();

		public Function( string name )
			: base( name ) { }

		// calls Resolve on all overloads
		public override int Resolve() {
			if( IsResolved )
				return 0;

			return unresolved = overloads.Sum( q => q.Resolve() );
		}
	}

	public class Overload {
		public Type			return_type;
		public List<Type>	parameter_types;
		public Function		function; // parent, the Function this Overload is part of
		public bool			is_member = false;
		
		public int			unresolved = -1;

		// calls Resolve on all Type 
		public int Resolve() {
			if( unresolved == 0 )
				return 0;

			return_type.parent						= function.parent;
			parameter_types.ForEach( q => q.parent	= function.parent );

			return unresolved = return_type.Resolve()
							  + parameter_types.Sum( q => q.Resolve() );
		}
	}

	public class Test {
		public static void DoTest()
		{
			var glob	= new Fragment( ""		) { kind = Fragment.Kind.Namespace };
			var my_ns	= new Fragment( "my"	) { kind = Fragment.Kind.Namespace };
			glob.Add( my_ns );
			var sub_ns	= new Fragment( "sub"	) { kind = Fragment.Kind.Namespace };
			my_ns.Add( sub_ns );

			// dies sind build in Types, keine Type-Usages... what to do...?
			glob.Add( new Fragment( "int"	) { kind = Fragment.Kind.Struct } );
			glob.Add( new Fragment( "double") { kind = Fragment.Kind.Struct } );

			glob.Add( new Fragment( "pow" ) {
				kind		= Fragment.Kind.Function,
				type		= "double",
				parameters	= new List<Fragment>(){
					new Fragment( "base" ) { type = "double"},
					new Fragment( "exp"	 ) { type = "double"}
				},
			} );

			var cls		= new Fragment( "vect" ) { kind = Fragment.Kind.Class };
			cls.Add( new Fragment( "top" ) {
				kind		= Fragment.Kind.Function,
				parameters	= new List<Fragment> {},
				type		= "int",
				is_member	= true
			} );

			cls.Add( new Fragment( "hans" ) { kind = Fragment.Kind.Field, type = "blib" } );
			my_ns.Add( cls );
			
			/*
			sub_ns.Add( new Fragment( "get_vect" ) {
				parameters	= new List<Fragment> {},
				return_type		= "vect"
			} );

			Console.WriteLine( "Resolving global: " + glob.Resolve() );

			var s1 = glob.Search( "my" );
			var s2 = (s1 as Hierarchic).Search( "sub" );

			IEnumerable<Hierarchic> using_namespaces = new List<Hierarchic>() { sub_ns };
			//using_namespaces.Add( sub_ns );

			// try to find "get_vect" in global scope
			var name = "get_vect";
			Named n = glob.SearchUp( name );
			if( n == null )
			{
				n = using_namespaces.SearchNS( name );
			}
			
			Console.WriteLine( name + " found: " + n );

			Console.WriteLine( "my::sub ? " + s2.FullName );
			*/
			/* we want to resolve different types,
			 * sometimes just a class, a namespace, both, a field, a member field
			 */
		}
	}
}
