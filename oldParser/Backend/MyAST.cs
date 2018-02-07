using System;
using System.Collections.Generic;
using System.Linq;
//using System.Text;

namespace JanSordid.MyLang.Backend {
	abstract public class MyNamed : MyBase {
		public readonly string			name;
		public readonly MyHierarchic	parent;

		public MyNamed( string name ) {
			this.name = name;
			this.parent = MyHierarchic.current;
			AddToParent();
		}

		public virtual void AddToParent() {
			parent.AddChild( this );
		}

		public override string ToString() {
			return parent.ToString() + "::" + name;
		}
	}

	abstract public class MyLocalScoped : MyNamed {
		public MyLocalScoped( string name )
			: base( name ) { }
	}

	abstract public class MyHierarchic : MyNamed {
		//-------------------------------------------------------------------------
		// static

		static public MyHierarchic current = null;

		static MyHierarchic() {
			MyHierarchic.current = MyGlobal.instance;
		}

		static public void ContextSet( MyHierarchic that ) {
			MyHierarchic.current = that;
		}

		//-------------------------------------------------------------------------
		// instance

		public readonly Dictionary<string, MyNamed> children
				  = new Dictionary<string, MyNamed>();

		public MyHierarchic( string name )
			: base( name ) {
			ContextOpen();
		}

		public MyHierarchic AddChild( MyNamed named ) {
			if( children.ContainsKey( named.name ) )
				throw new ArgumentException( "Duplicate child name: " + named + " in: " + this );

			children.Add( named.name, named );
			return this;
		}

		public void ContextUp() {
			MyHierarchic.current = current.parent;
		}

		public void ContextOpen() {
			MyHierarchic.current = this;
		}
	}

	abstract public class MyStructural : MyHierarchic {
		public Dictionary<string,MyField>	fields	= new Dictionary<string, MyField>();
		public Dictionary<string,Dictionary<MyParameters,MyFuncMeth>>	methods	= new Dictionary<string, Dictionary<MyParameters, MyFuncMeth>>();

		public MyStructural( string name )
			: base( name ) { }

		public void AddMethod( string name, MyParameters paras, MyFuncMeth funcmeth ) {
			if( !methods.ContainsKey( name ) )
			{
				methods.Add( name, new Dictionary<MyParameters, MyFuncMeth>() );
			}
			methods[name].Add( paras, funcmeth );
		}

		public void AddField( string p, MyField field ) {
			throw new NotImplementedException();
		}
	}

	public class MyGlobal : MyHierarchic {
		static public readonly MyGlobal instance = new MyGlobal();

		private MyGlobal()
			: base( "global" ) { }

		public override void AddToParent() { }

		public override string ToString() {
			return name;
		}
	}
}
