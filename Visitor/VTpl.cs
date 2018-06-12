﻿using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

using static Myll.MyllParser; // sadly pulls in all constants

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
			=> c?.tplArg().Select( VisitTplArg ).ToList()
			   ?? new List<TemplateArg>();

		public TemplateParam VisitTplParam( IdContext c )
		{
			TemplateParam tp = new TemplateParam {
				name = VisitId( c )
			};
			return tp;
		}

		public new List<TemplateParam> VisitTplParams( TplParamsContext c )
			=> c?.id().Select( VisitTplParam ).ToList()
			   ?? new List<TemplateParam>();
	}
}