using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using static System.String;

namespace Myll.Core
{
	using Attribs = Dictionary<string, List<string>>;

	public abstract class Node
	{
		public SrcPos srcPos = null!;

		[Pure]
		public override string ToString()
		{
			StringBuilder sb = GetType().GetProperties().Aggregate(
				new StringBuilder(),
				( builder, info ) => builder.AppendFormat(
					"{0}: {1}, ",
					info.Name,
					info.GetValue( this, null ) ?? "(null)" ) );

			sb.Length = Math.Max( sb.Length - 2, 0 );

			return Format( "{{{0} {1}}}", GetType().Name, sb );
		}
	}

	public abstract class AttributedNode : Node
	{
		protected Attribs attribs = new();

		public bool IsStatic      => HasAttrib( "static" );
		public bool IsCompileTime => HasAttrib( "ct" );
		public bool IsHidden      => HasAttrib( "hide" ) || HasAttrib( "hidden" );
		public bool IsNoInit      => HasAttrib( "noinit" ) || HasAttrib( "uninit" );

		public bool IsExtern    => HasAttrib( "extern" );
		public bool IsInline    => HasAttrib( "inline" );
		public bool IsVirtual   => HasAttrib( "virtual" );
		public bool IsConst     => HasAttrib( "const" );
		public bool IsPure      => HasAttrib( "pure" ) || HasAttrib( "const" );
		public bool IsMutable   => HasAttrib( "mutable" ) || HasAttrib( "mut" );
		// Static member functions cannot be const-qualified (C++ rejects it),
		// so the method-purity does not apply.
		public bool IsConstMethod => this is Decl decl && decl.IsInStruct && !IsStatic && IsPure && !IsMutable;
		public bool IsOverride  => HasAttrib( "override" );
		public bool IsNodiscard => HasAttrib( "nodiscard" );
		public bool IsNoexcept  => HasAttrib( "noexcept" );
		public bool IsImplicit  => HasAttrib( "implicit" );
		public bool IsDefault   => HasAttrib( "default" );
		public bool IsAbstract  => HasAttrib( "abstract" );
		public bool IsDisabled  => HasAttrib( "delete" ) || HasAttrib( "disallow" ) || HasAttrib( "disable" );
		public bool IsShadow    => HasAttrib( "shadow" );
		public bool IsUnshadow  => HasAttrib( "unshadow" );
		public bool IsFlags     => HasAttrib( "flags" );
		public bool IsOpBitwise => IsAttrib( "operators", "bitwise" );

public bool HasAttrib( string attrib )
			=> attribs.ContainsKey( attrib );

		public bool IsAttrib( string attrib, string value )
			=> attribs.TryGetValue( attrib, out List<string>? values )
		 && values.Contains( value );

		// Values of a value-carrying attribute ( [spec=all] ), in write order;
		// empty when the attribute is absent or valueless.
		public IReadOnlyList<string> AttribValues( string attrib )
			=> attribs.TryGetValue( attrib, out List<string>? values ) ? values : Array.Empty<string>();

		public virtual void AssignAttribs( Attribs inAttribs )
		{
			// Ownership by copy: fan-out consumers (MultiDecl, MultiStmt) must not share
			// the same dictionary, and later mutations (ExtractAccessAttribs) must not
			// leak into the caller's dictionary.
			attribs = new Attribs( inAttribs );
			foreach( KeyValuePair<string, List<string>> kv in attribs )
				attribs[kv.Key] = new List<string>( kv.Value );
		}

		public void MergeAttribs( Attribs incoming )
		{
			foreach( KeyValuePair<string, List<string>> kv in incoming ) {
				if( !attribs.ContainsKey( kv.Key ) )
					attribs[kv.Key] = kv.Value;
			}
		}
	}
}
