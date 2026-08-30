using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using static System.String;

namespace Myll.Core
{
	using Strings = List<string>;
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
		private Attribs attribs = new();

		public bool IsStatic      => HasAttrib( "static" );
		public bool IsCompileTime => HasAttrib( "ct" );
		public bool IsHidden      => HasAttrib( "hide" ) || HasAttrib( "hidden" );
		public bool IsNoInit      => HasAttrib( "noinit" ) || HasAttrib( "uninit" );

		public bool IsExtern    => HasAttrib( "extern" );
		public bool IsInline    => HasAttrib( "inline" );
		public bool IsVirtual   => HasAttrib( "virtual" );
		public bool IsConst     => HasAttrib( "const" );
		public bool IsPure      => HasAttrib( "pure" ) || HasAttrib( "const" );
		public bool IsOverride  => HasAttrib( "override" );
		public bool IsImplicit  => HasAttrib( "implicit" );
		public bool IsDefault   => HasAttrib( "default" );
		public bool IsDisabled  => HasAttrib( "disable" );
		public bool IsFlags     => HasAttrib( "flags" );
		public bool IsOpBitwise => IsAttrib( "operators", "bitwise" );

		public bool HasAttrib( string attrib )
			=> attribs.ContainsKey( attrib );

		public bool IsAttrib( string attrib, string value )
			=> attribs.TryGetValue( attrib, out Strings? values )
			&& values.Contains( value );

		public virtual void AssignAttribs( Attribs inAttribs )
		{
			attribs = inAttribs;
			AttribsAssigned();
		}

		protected virtual void AttribsAssigned() {}
	}
}
