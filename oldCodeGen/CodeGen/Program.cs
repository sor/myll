using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanSordid.MyLang.CodeGen
{
	class Program
	{
		static void Main( string[] args )
		{
			var global = new MyNamespace( "", null );

			var ns   = new MyNamespace( "JanSordid", global );
			var ns2   = new MyNamespace( "Testomania", ns );
			var cls  = new MyClass { name = "Blubb", ns = ns };
			var fld1 = new MyField { access_mod = MyAccessModifier.Private, name = "size", type = "int",     init = "0" };
			var fld2 = new MyField { access_mod = MyAccessModifier.Private, name = "data", type = "float[]", init = "new float(size)" };
			var ctor = new MyCtor  { access_mod = MyAccessModifier.Public,  is_default = false, body = "", parameters = { "int a", "float b" } };
			cls.Add( fld1 );
			cls.Add( fld2 );
			cls.Add( ctor );

			Console.WriteLine(
				cls.VisibleDeclaration( MyAccessModifier.None )
				);

			var enm = new MyEnum( ns2, "EnumByHand" );
			enm.Add( "None",	"99" );
			enm.Add( "First",	"1" );
			enm.Add( "Second",	"2" );

			var enm2 = new MyEnum( ns, "EnumAuto" );
			enm2.Add( "None" );
			enm2.Add( "First" );
			enm2.Add( "Second" );

			var enm3 = new MyEnum( ns, "EnumHybrid" );
			enm3.Add( "None" );
			enm3.Add( "First" );
			enm3.Add( "Second", "42" );
			enm3.Add( "Third" );

			Console.WriteLine(
				enm.VisibleDeclaration()
				);
			Console.WriteLine(
				enm2.VisibleDeclaration()
				);
			Console.WriteLine(
				enm3.VisibleDeclaration()
				);
			
			Console.WriteLine("\n///////////////////////////////" );
			
			Console.WriteLine(
				enm.ConcreteImplementation()
				);
			Console.WriteLine(
				enm2.ConcreteImplementation()
				);
			/*Console.WriteLine(
				enm3.ConcreteImplementation()
				);*/

			Console.ReadKey();
		}
	}
}
