using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;
using Myll.Resolver;

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
				tplArgs = VisitTplArgs( c.tplArgs() ),
			};
			return ret;
		}

		public new IdTplArgs VisitTypeIdTplArgs( TypeIdTplArgsContext c )
		{
			IdTplArgs ret = new() {
				id      = c.typeId().Visit(),
				tplArgs = VisitTplArgs( c.tplArgs() ),
			};
			return ret;
		}

		public new TplArg VisitTplArg( TplArgContext c )
		{
			TplArg ret;
			if( c.typespec()  != null ) ret = new TplArg { typespec = VisitTypespec( c.typespec() ) };
			else if( c.expr() != null ) {
				// Fold literal arithmetic right here (2*8 becomes 16), so spec
				// matching and emission compare plain constants; unfolded
				// expressions stay as written and C++ evaluates them.
				Expr lit = c.expr().Visit( Context );
				if( TemplateArgFolding.TryFoldLiteral( lit, out System.Numerics.BigInteger value ) )
					lit = new Literal { text = value.ToString() };
				ret = new TplArg { lit = lit };
			}
			else throw new Exception( "unknown template arg kind" );
			return ret;
		}

		public new List<TplArg> VisitTplArgs( TplArgsContext c )
			=> c?.tplArg().Select( VisitTplArg ).ToList()
			?? new List<TplArg>( 0 );

		public TplParam VisitTplParam( TplParamContext c )
		{
			if( c is NonTypeTplParamContext nonType ) {
				return new TplParam {
					name = nonType.id().Visit(),
					kind = TplParamKind.NonType,
					type = VisitTypespec( nonType.typespec() ),
				};
			}

			if( c is TypeTplParamContext type )
				return new TplParam {
					name   = type.id().Visit(),
					kind   = TplParamKind.Type,
					isPack = type.TRP_POINT() != null,
				};

			throw new Exception( "unknown template parameter kind" );
		}

		public new List<TplParam> VisitTplParams( TplParamsContext? c )
			=> c?.tplParam().Select( VisitTplParam ).ToList()
			?? new List<TplParam>();
	}
}
