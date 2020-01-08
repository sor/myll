using System;
using System.Windows.Forms;

using Antlr4.Runtime;

namespace Myll
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		public string testcase = @"#!/usr/bin/myll
[bitwise_ops]
enum Moep {
	A,
	B = 3,
	C
}
class Vec {
	field int a;
	method b(){}
}
func main(){
	if(a+b|8==c)
		(move)(a)(?b)(!d)c;
	else
		var int i = 9;
}";

		private void button1_Click(object sender, EventArgs e)
		{
			AntlrInputStream  inputStream       = new AntlrInputStream(textBox1.Text);
			MyllLexer         lexer             = new MyllLexer(inputStream);
			CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
			MyllParser        parser            = new MyllParser(commonTokenStream);
			//VisitorExtensions.AllVis.Visit( parser.prog() );
			VisitorExtensions.DeclVis.Visit( parser.prog() );
			//parser.levStmt().Visit();

			// HACK
			textBox2.Text = Output;
		}

		public static string Output { get; set; }
	}
}
