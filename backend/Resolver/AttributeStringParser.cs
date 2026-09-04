using System;
using System.Collections.Generic;
using Myll.Core;

namespace Myll.Resolver
{
	using Strings = List<string>;
	using Attribs = Dictionary<string, List<string>>;

	/// <summary>
	/// Parses a compact attribute string such as "rule_of_n=0, pub, operators(bitwise)"
	/// into the <see cref="Attribs"/> dictionary format used by <see cref="AttributedNode"/>.
	/// Outer square brackets are optional.
	/// </summary>
	public static class AttributeStringParser
	{
		public static Attribs Parse( string source )
		{
			Attribs ret = new();

			if( String.IsNullOrWhiteSpace( source ) )
				return ret;

			string trimmed = source.Trim();
			if( trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[trimmed.Length - 1] == ']' )
				trimmed = trimmed.Substring( 1, trimmed.Length - 2 ).Trim();

			foreach( string segment in trimmed.Split( ',' ) ) {
				string part = segment.Trim();
				if( String.IsNullOrEmpty( part ) )
					continue;

				(string key, string? value) = SplitAttribute( part );
				if( String.IsNullOrEmpty( key ) )
					continue;

				if( !ret.TryGetValue( key, out Strings? values ) ) {
					values = new Strings();
					ret.Add( key, values );
				}

				if( !String.IsNullOrEmpty( value ) )
					values.Add( value );
			}

			return ret;
		}

		private static (string Key, string? Value) SplitAttribute( string part )
		{
			int paren = part.IndexOf( '(' );
			int equal = part.IndexOf( '=' );

			if( paren != -1 && (equal == -1 || paren < equal) ) {
				string key   = part.Substring( 0, paren ).Trim();
				string value = part.Substring( paren + 1 ).TrimEnd( ')' ).Trim();
				return (key, value);
			}

			if( equal != -1 ) {
				string key   = part.Substring( 0, equal ).Trim();
				string value = part.Substring( equal + 1 ).Trim();
				return (key, value);
			}

			return (part, null);
		}
	}
}
