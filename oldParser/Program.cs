using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace JanSordid.MyLang
{
	class Program
	{
		static void DoIt( string source )
		{
			AntlrInputStream	input		= new AntlrInputStream( source );
			MyLangLexer			lexer		= new MyLangLexer( input );
			CommonTokenStream	tokens		= new CommonTokenStream( lexer );
			MyLangParser		parser		= new MyLangParser( tokens );
			IParseTree			tree		= parser.prog();
			Console.WriteLine( tree.ToStringTree( parser ).Replace( "(", "(" ) );
			Console.WriteLine( "-----" );
			int indent = 0;
			//*
			foreach ( var t in tree.ToStringTree( parser ) )
			{
				if ( t == '(' )
				{
					indent++;
					Console.Write( t + "\n" + new string( ' ', indent * 2 ) );
				}
				else if ( t == ')' )
				{
					indent--;
					Console.Write( "\n" + new string( ' ', indent * 2 ) + t );
				}
				else
					Console.Write( t );
			}//*/
			Console.WriteLine( "-----" );
			MyLangVisitor		visitor		= new MyLangVisitor();
			Console.WriteLine( visitor.Visit( tree ) );
		}

		static void Main( string[] args )
		{
			// field int a;		basic
			// field myclass b;	ID
			// field ns<type>::class<type,type OR const>

			DoIt( File.OpenText( "test.cpp" ).ReadToEnd() );
			while ( true )
			{
				Console.WriteLine( "Type expressions and then CTRL+Z" );
				StreamReader inputStream	= new StreamReader( Console.OpenStandardInput() );
				DoIt( inputStream.ReadToEnd() );
			}
		}
	}
}
