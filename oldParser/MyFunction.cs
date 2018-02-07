using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang
{
	/// <summary>
	/// used for fields, variables, constants and parameters
	/// </summary>
	public class MyNamedType : MyBase
	{
		public MyType	type;
		public string	name;

		public override string ToString()
		{
			return type.ToString() + " " + name;
		}
	}

}
