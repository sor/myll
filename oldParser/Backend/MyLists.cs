using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang.Backend {
	public class MyExpressions : MyBase {
		public List<MyExpression> list;
		public MyExpressions() { list = new List<MyExpression>(); }
		public MyExpressions( int capacity ) { list = new List<MyExpression>( capacity ); }
		public void Add( MyExpression s ) { list.Add( s ); }
		public override string ToString() { return string.Join( "  ", list.Select( q => q.ToString() ) ); }
	}

	public class MyParameters : MyBase {
		public List<MyNamedType> list;
		public MyParameters() { list = new List<MyNamedType>(); }
		public MyParameters( int capacity ) { list = new List<MyNamedType>( capacity ); }
		public void Add( MyNamedType s ) { list.Add( s ); }
		public string ToTypeString() { return string.Join( "  ", list.Select( q => q.type.ToString() ) ); }
		public override string ToString() { return string.Join( "  ", list.Select( q => q.ToString() ) ); }
	}

	public class MyFields : MyBase {
		public List<MyField> list;
		public MyFields() { list = new List<MyField>(); }
		public MyFields( int capacity ) { list = new List<MyField>( capacity ); }
		public void Add( MyField s ) { list.Add( s ); }
		public override string ToString() { return string.Join( "  ", list.Select( q => q.ToString() ) ); }
	}
}
