using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang.Backend
{
	class MyNamespace : MyHierarchic
	{
		public MyNamespace( string name )
			: base( name )
		{ }
	}

	class MyClass : MyStructural
	{
		public MyClass( string name )
			: base( name )
		{ }

		public string				base_class_names;

//		public MyList<MyStatic>		statics;
//		public MyList<MyConst>		consts;
		//public List<MyField>	fields	= new List<MyField>();
		//public List<MyMethod>	methods	= new List<MyMethod>();
		//public List<MyCtor>		ctors	= new List<MyCtor>();
		//public MyDtor			dtor;

		public void GatherIdentifier() {

			throw new NotImplementedException();
		}
	}

	public class MyField : MyNamed
	{
		public MyType		type;
		public MyExpression	init;

		public MyField( string name, MyType type )
			: base( name ) {
			this.type = type;
			this.init = null;
		}

		public MyField( string name, MyType type, MyExpression init )
			: base( name ) {
			this.type = type;
			this.init = init;
		}

		public override string ToString() {
			return "FIELD: " + type.ToString()
				+ " " + base.ToString()
				+ (init != null ? " = " + init.ToString() : "");
		}
	}

	class MyCtor : MyNamed // with signature?
	{
		public MyParameters parameters;
		public MyStatements	statements;

		public MyCtor( string name )
			: base( name )
		{ }

		public override string ToString()
		{
			return base.ToString() + "\n{ " + statements.ToString() + " }";
		}
	}

	public class MyFuncMeth : MyNamed // with signature?
	{
		public MyType		return_type;	// null == void ???
		public MyParameters	parameters;		// can be empty
		public MyStatements	statements;		// can be empty

		public MyFuncMeth( string name )
			: base( name )
		{}

		public override string ToString()
		{
			return base.ToString()
				+ "("	 + parameters.ToString() + ")"
				+ " -> " + return_type.ToString()
				+ " {\n" + statements.ToString()
				+ "\n}";
		}
	}
}
