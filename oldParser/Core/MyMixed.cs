using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLang.Core
{
	/// <summary>
	/// used for fields, variables, constants and parameters
	/// </summary>
	public class MyNamedType : IBase {
		public MyType	type;
		public string	name;

		public override string ToString() {
			return type.ToString() + " " + name;
		}
	}

	class MyVarDecl : MyStatement {
		public MyFields fields;
		public MyVarDecl( MyFields fields ) {
			this.fields = fields;
		}
		public override string ToString() {
			return fields.ToString();
		}
	}

}
