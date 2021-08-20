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
		public new IdTplArgs VisitIdTplArgs( IdTplArgsContext c )
		{
			IdTplArgs ret = new() {
				id      = c.id().Visit(),
				tplArgs = VisitTplArgs( c.tplArgs() )
			};
			return ret;
		}

		public new TplArg VisitTplArg( TplArgContext c )
		{
			TplArg ret;
			if( c.typespec()  != null ) ret = new TplArg { typespec = VisitTypespec( c.typespec() ) };
			else if( c.lit()  != null ) ret = new TplArg { lit      = c.lit().Visit() };
			else throw new Exception( "unknown template arg kind" );
			return ret;
		}

		public new List<TplArg> VisitTplArgs( TplArgsContext c )
			=> c?.tplArg().Select( VisitTplArg ).ToList()
			?? new List<TplArg>( 0 );

		public TplParam VisitTplParam( IdContext c )
		{
			TplParam tp = new() {
				name = c.Visit(),
			};
			return tp;
		}

		public new List<TplParam> VisitTplParams( TplParamsContext c )
			=> c?.id().Select( VisitTplParam ).ToList()
			?? new List<TplParam>();
	}
}
