using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLang.Core {
	interface IBase { }

	abstract public class MyStatement : IBase {
		virtual public IEnumerable<string> ToStringList( string I = "  " ) {
			return new List<string>( 1 ) { I + ToString() };
		}
		//abstract public IEnumerable<string> ToStringList( string I = "  " );
	}
	abstract public class MyExpression : IBase { }

	enum Assoc { Left, Right };
}
