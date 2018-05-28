using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Myll.Core;

using Array = Myll.Core.Array;
using Enum = Myll.Core.Enum;
using Parser = Myll.MyllParser;

using static Myll.MyllParser;	// sadly pulls in all constants

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		public new IdentifierTpl VisitIdTplArgs( IdTplArgsContext c )
		{
			IdentifierTpl ret = new IdentifierTpl {
				name         = VisitId( c.id() ),
				templateArgs = VisitTplArgs( c.tplArgs() )
			};
			return ret;
		}

		public new TemplateArg VisitTplArg( TplArgContext c )
		{
			TemplateArg ret;
			if( c.typespec()  != null ) ret = new TemplateArg { type = VisitTypespec( c.typespec() ) };
			else if( c.id()   != null ) ret = new TemplateArg { name = VisitId( c.id() ) };
			else if( c.expr() != null ) ret = new TemplateArg { expr = c.expr().Visit() };
			else throw new Exception( "unknown template arg kind" );
			return ret;
		}

		public new List<TemplateArg> VisitTplArgs( TplArgsContext c )
		{
			List<TemplateArg> ret = c?.tplArg().Select( VisitTplArg ).ToList();
			return ret;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public new string VisitId( IdContext c )
		{
			return c.GetText();
		}

		public TemplateParam VisitTplParam( IdContext c )
		{
			TemplateParam tp = new TemplateParam {
				name = VisitId( c )
			};
			return tp;
		}

		public new List<TemplateParam> VisitTplParams( TplParamsContext c )
		{
			List<TemplateParam> ret = c.id().Select( VisitTplParam ).ToList();
			return ret;
		}
	}

	public partial class Visitor
		: ExtendedVisitor<object>
	{
		private void Visit( TerminalNodeImpl node )
		{
			Console.WriteLine( " Visit Symbol={0}", node.Symbol.Text );
		}

		public override object VisitProg( ProgContext context )
		{
			Console.WriteLine( "HelloVisitor VisitR" );
			context
				.children
				//.OfType<TerminalNodeImpl>()
				.ToList()
				.ForEach( child => Visit( child ) );
			return null;
		}
	}
}
