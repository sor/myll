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
			MyllVisitor       vis               = new MyllVisitor();
			MyllExprVisitor   exprVis           = new MyllExprVisitor();
			MyllStmtVisitor   stmtVis           = new MyllStmtVisitor();
			vis.TellVisitors(vis, exprVis, stmtVis);
			exprVis.TellVisitors(vis, exprVis, stmtVis);
			stmtVis.TellVisitors(vis, exprVis, stmtVis);
			vis.VisitStmt(parser.stmt());
		}
	}
}