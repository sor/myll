using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MyLang.Core;

namespace MyLang {
	class Generator {
		readonly string I = "  ";	// indent string
		List<string> declaration	= new List<string>();
		List<string> definition		= new List<string>();
		List<string> inline			= new List<string>();

		string Generate( MyType type, string name ) {
			return type.ToString() + " " + name;	// TODO: embed name correctly
		}

		string Generate( MyParameters paras ) {
			var l = paras.list.Select( q => Generate( q.type, q.name ) );
			return "( " + string.Join( ", ", l ) + " )";
		}

		IEnumerable<string> Generate( MyStatement stmt, string II = "" ) {
			return stmt.ToStringList( I ).Select( q => II + q );
		}

		public void Generate( MyClass cls, string II = "" ) {
			Dictionary<string,MyExpression> initializer = new Dictionary<string, MyExpression>();

			declaration.Add( II + "class " + cls.name + " {" );

			declaration.Add( II + I + "public:" );

			declaration.Add( II + I + I + "// Field(s)" );
			foreach( var f in cls.fields.list )
			{
				declaration.Add( II + I + I + Generate( f.type, f.name ) + ";" );
				if( f.init != null )
					initializer.Add( f.name, f.init );
			}

			declaration.Add( "" );
			declaration.Add( II + I + I + "// Constructor(s)" );
			foreach( var ctors in cls.methods.Where( q => q.Key == "ctor" )
											.Select( q => q.Value ) )
			{
				foreach( var ctor in ctors )
				{
					HashSet<MyStatement> skip_list = new HashSet<MyStatement>();

					string	paras		= Generate( ctor.Key );
					var		overload	= ctor.Value;
					var		exsts		= overload.statements.list.OfType<MyExpressionStatement>();

					foreach( var exst in exsts )
					{
						var expr = exst.expr;
						var b_ex = exst.expr as Core.BinOpExpr;

						if( b_ex != null )
						{
							skip_list.Add( exst );

							List<MyExpression> l = new List<MyExpression>();
							while( b_ex != null
								&& b_ex.operation == "=" )
							{
								l.Add( b_ex.left );
								b_ex = b_ex.right as Core.BinOpExpr;
							}
							l.Add( b_ex );

							for( int i = l.Count - 2; i >= 0; --i )
							{
								var id = l[i] as MyId;
								if( id == null )
									Console.WriteLine( "not an ID:" + l[i].ToString() );
								else
								{
									if( initializer.ContainsKey( id.value ) )
									{
										Console.WriteLine( "removing " + id.value + " was: " + initializer[id.value] + " now: " + l[i + 1] );
										initializer.Remove( id.value );
									}
									initializer.Add( id.value, l[i + 1] );
								}
							}
						}
					}
					declaration.Add( II + I + I + cls.name + paras + ";" );

					definition.Add( cls.name + "::" + cls.name + paras );
					if( initializer.Count > 0 )
					{
						int not_last = initializer.Count;
						definition.Add( I + ":" );
						foreach( var q in initializer )
						{
							--not_last;
							definition.Add( I + I + q.Key + "( " + q.Value.ToString() + " )" + (not_last == 0 ? "" : ",") );
						}
						//initializer.Select( q => I + I + I + q.Key + "( " + q.Value.ToString() + " )" ) );
					}
					var body = ctor.Value.statements.list
									.Where( q => !skip_list.Contains( q ) );

					definition.Add( "{" );
					definition.AddRange(
						body.SelectMany( q => Generate( q, II + I ) ) );
					definition.Add( "}" );
				}
			}

			declaration.Add( "" );
			declaration.Add( I + I + "// Method(s)" );
			foreach( var meths in cls.methods.Where( q => q.Key != "ctor" ) )
			{
				foreach( var meth in meths.Value )
				{
					string	paras		= Generate( meth.Key );
					var		overload	= meth.Value;
					var		exsts		= overload.statements.list.OfType<MyExpressionStatement>();

					declaration.Add( I + I + overload.return_type + " " + meths.Key + paras + ";" );

					definition.Add( overload.return_type + " " + cls.name + "::" + meths.Key + paras );

					var body = overload.statements.list;

					definition.Add( "{" );
					definition.AddRange(
						body.SelectMany( q => Generate( q, II ) ) );
					definition.Add( "}" );
				}
			}

			declaration.Add( "};" );
		}

		public override string ToString() {
			return
				"// .hpp\n" + string.Join( "\n", declaration ) + "\n\n" +
				"// .cpp\n" + string.Join( "\n", definition ) + "\n\n" +
				"// .inl.cpp\n" + string.Join( "\n", inline );
		}
/*
 * klassen variablen: name, d2 (rausfinden was dieser typ kann, hat und wie er parametriert wird)
 * methoden name: MyGenerator
 * methoden parameter: name
 * IRRELEVANT lokale variablen: { d {i} {i} }
 */
	}
}
