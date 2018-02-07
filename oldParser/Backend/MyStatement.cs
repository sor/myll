using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang.Backend {

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
