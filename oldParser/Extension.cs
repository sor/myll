using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MyLang.Core;

namespace MyLang
{
	static class Extension
	{
		public static
		TElement FirstResult<TSource,TElement>(
				this IEnumerable<TSource> source,
				Func<TSource, TElement> predicate ) {
			foreach( var it in source )
			{
				TElement ret = predicate.Invoke( it );
				if( ret != null )
					return ret;
			}
			return default(TElement);
		}

		static readonly Dictionary<int, MyBasicType.Signedness> typeToSign =
					new Dictionary<int, MyBasicType.Signedness> {
			{	MyLangParser.UNSIGNED,	MyBasicType.Signedness.Unsigned	},
			{	MyLangParser.SIGNED,	MyBasicType.Signedness.Signed	},
		};

		public static
		MyBasicType.Signedness ToSignedness( this MyLangParser.SignQualifierContext ctx )
		{
			if ( ctx == null )	return MyBasicType.Signedness.DontCare;
			else				return typeToSign[ctx.qual.Type];
		}

		public static
		string IDToString( this Antlr4.Runtime.Tree.ITerminalNode node )
		{
			return node.Symbol.Text;
		}

		// Output Generation
		// Enums

		public static
		string Gen( this MyBasicType.Type self )
		{
			switch( self )
			{
				case MyBasicType.Type.LongDouble:	return "long double";
				case MyBasicType.Type.LongLong:		return "long long";
				default:							return self.ToString().ToLower();
			}
		}

		public static
		string Gen( this MyBasicType.Signedness self )
		{
			switch( self )
			{
				case MyBasicType.Signedness.DontCare:	return string.Empty;
				default:								return self.ToString().ToLower();
			}
		}
	}
}
