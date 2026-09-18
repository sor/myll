using System.Collections.Generic;
using System.Linq;

namespace Myll.Core
{
	/// <summary>
	/// A single line of generated C++ code together with the optional Myll source
	/// position it was emitted from. Used by the source-map feature to emit
	/// <c>#line</c> directives.
	/// </summary>
	public sealed class SourceLine
	{
		public string  Text   { get; }
		public SrcPos? SrcPos { get; set; }

		public SourceLine( string text, SrcPos? srcPos = null )
		{
			Text   = text;
			SrcPos = srcPos;
		}

		public static implicit operator SourceLine( string text ) => new( text );

		public SourceLine WithSrcPos( SrcPos? srcPos ) => new( Text, srcPos );

		public SourceLine WithText( string text ) => new( text, SrcPos );
	}

	/// <summary>
	/// A list of generated C++ lines. This is the dedicated type that replaces the
	/// old <c>using Strings = List&lt;string&gt;</c> alias used throughout the
	/// generator.
	/// </summary>
	public class SourceLines : List<SourceLine>
	{
		public SourceLines() {}
		public SourceLines( IEnumerable<SourceLine> collection ) : base( collection ) {}
		public SourceLines( IEnumerable<string> collection )     : base( collection.Select( s => new SourceLine( s ) ) ) {}
		public SourceLines( params string[] lines )              : base( lines.Select( s => new SourceLine( s ) ) ) {}

		public void Add( string text ) => base.Add( new SourceLine( text ) );

		public void AddRange( IEnumerable<string> collection )
			=> base.AddRange( collection.Select( s => new SourceLine( s ) ) );

		public IEnumerable<string> Texts => this.Select( l => l.Text );
	}
}
