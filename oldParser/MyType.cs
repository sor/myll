using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang
{
	abstract public class MyType : MyBase
	{
		public MyQualifier				qualifier	= MyQualifier.None;
		public IEnumerable<MyPointer>	ptr			= Enumerable.Empty<MyPointer>();

		public override string ToString()
		{
			var ret = (qualifier.Gen() + " " + SubGen()).TrimStart(' ');
			foreach ( var p in ptr )
			{
				ret = p.ToString( ret );
			}
			return ret;
		}

		protected abstract string SubGen();
	}

	public class MyBasicType : MyType
	{
		public enum Type
		{
			Void,
			Bool,
			Float,
			Double,
			LongDouble,
			Char,
			Short,
			Int,
			Long,
			LongLong
		}

		public enum Signedness
		{
			DontCare,
			Signed,
			Unsigned
		}

		public Signedness	sign = Signedness.DontCare;
		public Type			type;

		public MyBasicType() {}
		public MyBasicType( MyBasicType.Type type )
		{
			this.type = type;
		}

		protected override string SubGen()
		{
			if ( sign == Signedness.DontCare )	return						type.Gen();
			else								return sign.Gen() + " " +	type.Gen();
		}
	}

	// ns::ns<...>::name<tpl,tpl>
	public class MyAdvancedType : MyType
	{
		public IEnumerable<MySegment>	segments;

		protected override string SubGen()
		{
			return string.Join( "::", segments.Select( q => q.ToString() ) );
		}
	}
}
