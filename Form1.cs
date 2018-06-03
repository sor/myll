using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

		private void button1_Click(object sender, EventArgs e)
		{
			AntlrInputStream  inputStream       = new AntlrInputStream(textBox1.Text);
			MyllLexer         lexer             = new MyllLexer(inputStream);
			CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
			MyllParser        parser            = new MyllParser(commonTokenStream);
			//VisitorExtensions.AllVis.Visit( parser.prog() );
			VisitorExtensions.DeclVis.Visit( parser.prog() );
			//parser.levStmt().Visit();
		}
	}
}
