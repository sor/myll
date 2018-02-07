using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang
{
	static class Extension
	{

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
		string Gen( this MyQualifier self )
		{
			if ( self == MyQualifier.None )
				return string.Empty;
			
			List<string> ret = new List<string>( 3 );
			if ( self.HasFlag( MyQualifier.Const ) )	ret.Add( "const" );
			if ( self.HasFlag( MyQualifier.Volatile ) )	ret.Add( "volatile" );
			if ( self.HasFlag( MyQualifier.Mutable ) )	ret.Add( "mutable" );
			return string.Join( " ", ret );
		}

		public static
		string Gen( this MyBasicType.Type self )
		{
			switch (self)
			{
				case MyBasicType.Type.LongDouble:	return "long double";
				case MyBasicType.Type.LongLong:		return "long long";
				default:							return self.ToString().ToLower();
			}
		}

		public static
		string Gen( this MyBasicType.Signedness self )
		{
			switch (self)
			{
				case MyBasicType.Signedness.DontCare:	return string.Empty;
				default:								return self.ToString().ToLower();
			}
		}
	}
}
