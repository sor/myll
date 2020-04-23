using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

using static Myll.MyllParser; // sadly pulls in all constants

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		public new IdTpl VisitIdTplArgs( IdTplArgsContext c )
		{
			IdTpl ret = new IdTpl {
				id      = c.id().Visit(),
				tplArgs = VisitTplArgs( c.tplArgs() )
			};
			return ret;
		}

		public new TemplateArg VisitTplArg( TplArgContext c )
		{
			TemplateArg ret;
			if( c.typespec()  != null ) ret = new TemplateArg { typespec = VisitTypespec( c.typespec() ) };
			else if( c.lit()  != null ) ret = new TemplateArg { lit      = c.lit().Visit() };
			else throw new Exception( "unknown template arg kind" );
			return ret;
		}

		public new List<TemplateArg> VisitTplArgs( TplArgsContext c )
			=> c?.tplArg().Select( VisitTplArg ).ToList()
			?? new List<TemplateArg>( 0 );

		public TplParam VisitTplParam( IdContext c )
		{
			TplParam tp = new TplParam {
				name = c.Visit(),
			};
			return tp;
		}

		public new List<TplParam> VisitTplParams( TplParamsContext c )
			=> c?.id().Select( VisitTplParam ).ToList()
			?? new List<TplParam>();
	}
}
