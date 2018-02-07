using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanSordid.MyLang.CodeGen
{
	class MyNamed
	{
		public readonly string			name;
		public readonly MyHierarchic	parent;	// null if global

		public MyNamed( string name, MyHierarchic parent )
		{
			this.name	= name;
			this.parent	= parent;
			if( parent != null )
				parent.Add( this );
		}

		public override string ToString()
		{
			return base.ToString() + " " + name;
		}
	}

	class MyHierarchic : MyNamed
	{
		public readonly Dictionary<string, MyNamed> children
		 = new Dictionary<string, MyNamed>();

		public MyHierarchic( string name, MyHierarchic parent )
			: base( name, parent )
		{}

		public MyHierarchic Add( MyNamed named )
		{
			if( children.ContainsKey( named.name ) )
				throw new ArgumentException( "Duplicate child name: " + named + " in: " + this );
			
			children.Add( named.name, named );
			return this;
		}
	}

	[Flags]
	enum MyAccessModifier
	{
		None		= 0,
		Public		= 1 << 0,
		Protected	= 1 << 1,
		Private		= 1 << 2
	}

	interface IMyLang
	{
		string VisibleDeclaration( MyAccessModifier am );
		string InlineImplementation( MyAccessModifier am );
		string ConcreteImplementation( MyAccessModifier am );
		string HiddenDeclaration( MyAccessModifier am );		// PIMPL
		string HiddenImplementation( MyAccessModifier am );		// PIMPL
	}

	class MyNamespace : MyHierarchic
	{
		public Stack<string> ns_stack = new Stack<string>();

		private bool HasNS { get { return name != "::" && name.Length > 0; } }

		public string Name { get { return name == "::" ? "::" : name.Length == 0 ? "" : name + "::"; } }

		public string StartNS	{ get { return HasNS ? "namespace " + name + " {"	: ""; } }
		public string EndNS		{ get { return HasNS ? "}"							: ""; } }

		public MyNamespace( string name, MyHierarchic parent )
			: base( name, parent )
		{}
	}

	class MyClassMember
	{
		public MyClass			containing_class;
		public MyAccessModifier	access_mod = MyAccessModifier.None;
	}

	class MyList<T> : List<T>, IMyLang
		where T : MyClassMember, IMyLang
	{
		public void Add( T item, MyClass cls )
		{
			item.containing_class = cls;
			Add( item );
		}

		public string VisibleDeclaration( MyAccessModifier am ) 	{ return this.Aggregate<T, string>( string.Empty, ( accu, t ) => accu + t.VisibleDeclaration( am ) ); }
		public string InlineImplementation( MyAccessModifier am ) 	{ return this.Aggregate<T, string>( string.Empty, ( accu, t ) => accu + t.InlineImplementation( am ) ); }
		public string ConcreteImplementation( MyAccessModifier am ) { return this.Aggregate<T, string>( string.Empty, ( accu, t ) => accu + t.ConcreteImplementation( am ) ); }
		public string HiddenDeclaration( MyAccessModifier am ) 		{ return this.Aggregate<T, string>( string.Empty, ( accu, t ) => accu + t.HiddenDeclaration( am ) ); }
		public string HiddenImplementation( MyAccessModifier am ) 	{ return this.Aggregate<T, string>( string.Empty, ( accu, t ) => accu + t.HiddenImplementation( am ) ); }
	}

	class MyField : MyClassMember, IMyLang
	{
		public string type;
		public string name;
		public string init;

		public string I { get { return new string( ' ', 8 ); } }

		public string AsInitializer { get { return name + "( " + init + " )"; } }

		public string VisibleDeclaration( MyAccessModifier am )
		{
			if( !access_mod.HasFlag( am ) )
				return string.Empty;

			return I + type + " " + name + ";\n";
		}
		public string InlineImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
		public string ConcreteImplementation( MyAccessModifier am ) { throw new NotImplementedException(); }
		public string HiddenDeclaration( MyAccessModifier am ) 		{ throw new NotImplementedException(); }
		public string HiddenImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
	}

	class MyCtor : MyClassMember, IMyLang
	{
		public bool				is_default;
		public bool				is_delete;

		public List<string>		parameters = new List<string>();
		public string			body;

		public Dictionary<string,string> initializer = new Dictionary<string,string>();

		public string I { get { return new string( ' ', 8 ); } }
		
		public string BuildParams		{ get { return string.Join( ", ", parameters ); } }
		public string BuildSignature	{ get { return I + "ctor(" + BuildParams + ")"; } }

		public string VisibleDeclaration( MyAccessModifier am )
		{
			if( !access_mod.HasFlag( am ) )
				return string.Empty;

			if( is_default )
				return BuildSignature + " = default;\n";
			else if( is_delete )
				return BuildSignature + " = delete;\n";

			string initializers = string.Empty;
			var fields = containing_class.fields.Where( q => q.init != null );
			if( fields.Count() > 0 )
			{
				initializers = I
					+ "  : "
					+ string.Join(	",\n" + I + "    ",
									fields.Select( q => q.AsInitializer ) )
					+ "\n";
			}

			return BuildSignature + "\n"
				+ initializers
				+ I + "{\n"
				+ I + "    " + body.Replace( "\n", I + "    \n" ) + "\n"
				+ I + "};\n";
		}
		public string InlineImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
		public string ConcreteImplementation( MyAccessModifier am ) { throw new NotImplementedException(); }
		public string HiddenDeclaration( MyAccessModifier am ) 		{ throw new NotImplementedException(); }
		public string HiddenImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
	}

	class MyDtor : MyClassMember, IMyLang
	{
		public string VisibleDeclaration( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
		public string InlineImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
		public string ConcreteImplementation( MyAccessModifier am ) { throw new NotImplementedException(); }
		public string HiddenDeclaration( MyAccessModifier am ) 		{ throw new NotImplementedException(); }
		public string HiddenImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
	}

	class MyMethod : MyClassMember, IMyLang
	{
		public string VisibleDeclaration( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
		public string InlineImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
		public string ConcreteImplementation( MyAccessModifier am ) { throw new NotImplementedException(); }
		public string HiddenDeclaration( MyAccessModifier am ) 		{ throw new NotImplementedException(); }
		public string HiddenImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
	}

	class MyClass : IMyLang
	{
		public MyNamespace			ns;
		public string				name;
		public string				base_class_names;

//		public MyList<MyStatic>		statics;
//		public MyList<MyConst>		consts;
		public MyList<MyField>		fields	= new MyList<MyField>();
		public MyList<MyMethod>		methods	= new MyList<MyMethod>();
		public MyList<MyCtor>		ctors	= new MyList<MyCtor>();
		public MyDtor				dtor;
//		public MyList<MyOperator>	operators;

		public void Add( MyField  o ) { fields .Add( o, this ); }
		public void Add( MyMethod o ) { methods.Add( o, this ); }
		public void Add( MyCtor   o ) { ctors  .Add( o, this ); }
		public void Add( MyDtor   o ) { o.containing_class = this; dtor = o; }

		public string VisibleDeclaration( MyAccessModifier am )
		{
			string derived_from = string.IsNullOrEmpty( base_class_names )
				? string.Empty
				: (" : " + base_class_names);

			string ret =  "namespace " + ns.name + " {\n"
						+ "    class " + name + derived_from + " {\n"
						+          fields.VisibleDeclaration( MyAccessModifier.Private )
						+          ctors.VisibleDeclaration( MyAccessModifier.Private )
						+ "      protected:\n"
						+          fields.VisibleDeclaration( MyAccessModifier.Protected )
						+          ctors.VisibleDeclaration( MyAccessModifier.Protected )
						+ "      public:\n"
						+          fields.VisibleDeclaration( MyAccessModifier.Public )
						+          ctors.VisibleDeclaration( MyAccessModifier.Public )
						+ "    };\n"
						+ "}";

			return ret;
		}
		public string InlineImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
		public string ConcreteImplementation( MyAccessModifier am ) { throw new NotImplementedException(); }
		public string HiddenDeclaration( MyAccessModifier am ) 		{ throw new NotImplementedException(); }
		public string HiddenImplementation( MyAccessModifier am ) 	{ throw new NotImplementedException(); }
	}
}
