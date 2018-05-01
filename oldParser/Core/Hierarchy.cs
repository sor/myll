using System;
using System.Collections.Generic;
using System.Linq;
//using System.Text;

namespace MyLang.Core {

	abstract public class MyNamed : IBase
	{
		public abstract Library.Named LibNam { get; }

		public readonly string			name;
		public readonly MyHierarchic	parent;
		public int						order;

		public MyNamed( string name, MyHierarchic parent ) {
			this.name	= name;
			this.parent	= parent;
			AddToParent();
		}

		public virtual void AddToParent() {
			parent.AddChild( this );
		}

		public override string ToString() {
			return parent.ToString() + "::" + name;
		}
	}

	//abstract public class MyLocalScoped : MyNamed {
	//	public MyLocalScoped( string name, MyHierarchic parent )
	//		: base( name, parent ) { }
	//}

	abstract public class MyHierarchic : MyNamed {
		//-------------------------------------------------------------------------
		// instance

		public abstract Library.Hierarchic	LibHier	{ get; }
		public override Library.Named		LibNam	{ get { return LibHier; } }

		public MyFields		fields	= new MyFields();
		public MyProperties	props	= new MyProperties();
		public Dictionary<string,Dictionary<MyParameters,MyFuncMeth>>
							methods	= new Dictionary<string, Dictionary<MyParameters, MyFuncMeth>>();
		public int current_order = 1;

		public readonly Dictionary<string, MyNamed> children
				  = new Dictionary<string, MyNamed>();

		public MyHierarchic( string name, MyHierarchic parent )
			: base( name, parent ) { }

		public MyHierarchic AddChild( MyNamed named )
		{
			if( children.ContainsKey( named.name ) )
				 throw new ArgumentException( "Duplicate child name: " + named + " in: " + this );

			children.Add( named.name, named );
			return this;
		}

		//-------------------------------------------------------------------------
		// lookup
		
		/*
		public MyNamed ContainsType( string name ) {
			MyNamed value;
			children.TryGetValue( name, out value );
			return value;
		}

		public virtual MyNamed ContainsTypeTree( string name ) {
			var a = ContainsType( name );

			if( a != default( MyNamed ) )
				return a;

			return parent.ContainsTypeTree( name );
		}
		*/

		////// von Structural

		public void AddMethod( string name, MyParameters paras, MyFuncMeth funcmeth ) {
			funcmeth.order = current_order++;
			if( !methods.ContainsKey( name ) )
			{
				methods.Add( name, new Dictionary<MyParameters, MyFuncMeth>() );
			}
			methods[name].Add( paras, funcmeth );
		}

		public void AddProperty( MyProperty prop ) {
			prop.order = current_order++;
			props.Add( prop );
			// TODO: noch was zu machen hier?
		}

		public void AddField( MyField field ) {
			field.order = current_order++;
			fields.Add( field );
		}
	}

	public class MyGlobal : MyHierarchic {
		#region STATIC
		static public readonly MyGlobal instance = new MyGlobal();
		static public Library.Hierarchic library;
		#endregion

		Library.Hierarchic lib;
		public override Library.Hierarchic LibHier { get { return lib; } }

		public MyGlobal() : base( "global", null ) {
			lib = new Library.Hierarchic( name );
			//parent.LibHier.Add( lib );
		}

		// no parent available
		public override void AddToParent() { }

		// the tree ends here
		//public override MyNamed ContainsTypeTree( string name ) {
		//	return ContainsType( name );
		//}

		public override string ToString() {
			return name;
		}
	}

	public class MyNamespace : MyHierarchic {
		Library.Hierarchic lib;
		public override Library.Hierarchic LibHier { get { return lib; } }

		public MyNamespace( string name, MyHierarchic parent )
			: base( name, parent ) {
			lib = new Library.Hierarchic( name );
			parent.LibHier.Add( lib );
		}
	}

	public class MyClass : MyHierarchic {
		Library.Structural lib;
		public override Library.Hierarchic LibHier { get { return lib; } }

		public MyClass( string name, MyHierarchic parent, MyTemplateArgs template_params )
			: base( name, parent )
		{
			lib = new Library.Structural( name, Library.Structural.Kind.Class );
			parent.LibHier.Add( lib );
		}

		public string				base_class_names;

		//		public MyList<MyStatic>		statics;
		//		public MyList<MyConst>		consts;
		//public List<MyField>		fields	= new List<MyField>();
		//public List<MyMethod>		methods	= new List<MyMethod>();
		//public List<MyCtor>		ctors	= new List<MyCtor>();
		//public MyDtor				dtor;

		public void GatherIdentifier() {

			throw new NotImplementedException();
		}
	}

	public class MyProperty : MyNamed {
		Library.Field lib;
		public override Library.Named LibNam { get { return lib; } }

		[Flags]
		enum Operators {
			Get		= 1,
			ConstRef= 2,
			Ref		= 4,
			Set		= 8
		}

		public MyType		type;
		public MyExpression	init;

		public MyProperty( string name, MyHierarchic parent, MyType type )
			: this( name, parent, type, null ) { }

		public MyProperty( string name, MyHierarchic parent, MyType type, MyExpression init )
			: base( name, parent ) {
			lib = new Library.Field( name, Library.Field.Kind.Field ) { type = "" };
			parent.LibHier.Add( lib );

			this.type = type;
			this.init = init;
		}

		public override string ToString() {
			return "PROPERTY: " + type.ToString()
				+ " " + base.ToString()
				+ (init != null ? " = " + init.ToString() : "");
		}
	}

	public class MyField : MyNamed {
		Library.Field lib;
		public override Library.Named LibNam { get { return lib; } }

		public MyType		type;
		public MyExpression	init;

		public MyField( string name, MyHierarchic parent, MyType type )
			: this( name, parent, type, null ) { }

		public MyField( string name, MyHierarchic parent, MyType type, MyExpression init )
			: base( name, parent ) {
			this.type = type;
			this.init = init;
		}

		public override string ToString() {
			return "FIELD: " + type.ToString()
				+ " " + base.ToString()
				+ (init != null ? " = " + init.ToString() : "");
		}
	}

	public class MyCtor : MyNamed // with signature?
	{
		Library.Function lib;
		public override Library.Named LibNam { get { return lib; } }

		public MyParameters parameters;
		public MyStatements	statements;

		public MyCtor( string name, MyHierarchic parent )
			: base( name, parent ) { }

		public override string ToString() {
			return base.ToString() + "\n{ " + statements.ToString() + " }";
		}
	}

	public class MyFuncMeth : MyHierarchic /*MyNamed*/ // with signature?
	{
		Library.Function lib;
		public override Library.Hierarchic LibHier { get { return lib; } }
		//public override Library.Named LibNam { get { return lib; } }

		public MyType		return_type = new MyBasicType( MyBasicType.Type.Void );	// null == void ???
		public MyParameters	parameters;		// can be empty
		public MyStatements	statements;		// can be empty

		public MyFuncMeth( string name, MyHierarchic parent )
			: base( name, parent )
		{
			lib = new Library.Function( name );
			parent.LibHier.Add( lib );
		}

		public override string ToString() {
			return base.ToString()
				+ "(" + parameters.ToString() + ")"
				+ " -> " + return_type.ToString()
				+ " {\n" + statements.ToString()
				+ "\n}";
		}
	}
}
