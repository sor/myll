using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanSordid.MyLang
{
	[Flags]
	public enum MyQualifier
	{
		None		= 0,
		Const		= 1,
		Volatile	= 2,
		Mutable		= 4, // Anti-Const?
	}

	public class MyPointer : MyBase
	{
		public enum Type
		{
			Raw,
			LVRef,
			RVRef,
			RawArray,
			Unique,
			Shared,
			Weak,
			Array,
			Vector,
			Set,
			OrderedSet,
			MultiSet,
			OrderedMultiSet,
		}

		readonly Dictionary<Type,string> template = new Dictionary<Type, string>
		{
			{ Type.Raw,				"{0} * {1}" },
			{ Type.LVRef,			"{0} & {1}" },
			{ Type.RVRef,			"{0} && {1}" },
			{ Type.RawArray,		"{0}[{2}] {1}" },		// TODO: named types must be embedded
			{ Type.Unique,			"std::unique_ptr<{0}> {1}" },
			{ Type.Shared,			"std::shared_ptr<{0}> {1}" },
			{ Type.Weak,			"std::weak_ptr<{0}> {1}" },
			{ Type.Array,			"std::array<{0},{2}> {1}" },
			{ Type.Vector,			"std::vector<{0}> {1}" },
			{ Type.Set,				"std::unordered_set<{0}> {1}" },
			{ Type.OrderedSet,		"std::set<{0}> {1}" },
			{ Type.MultiSet,		"std::unordered_multiset<{0}> {1}" },
			{ Type.OrderedMultiSet,	"std::multiset<{0}> {1}" },
		};

		public MyQualifier	qualifier = MyQualifier.None;
		public Type			type;
		public MyType		sub_type; // TODO: noch unbenutzt
		public string		content;

		public string ToString( string inner )
		{
			if     ( content  != null )	return string.Format( template[type], inner, qualifier.Gen(), content  ).TrimEnd(' ');
			else if( sub_type != null )	return string.Format( template[type], inner, qualifier.Gen(), sub_type ).TrimEnd(' ');
			else						return string.Format( template[type], inner, qualifier.Gen()           ).TrimEnd(' ');
		}
	}

	public class MySegment : MyBase
	{
		public string						name;
		public IEnumerable<MyTemplateParam>	template_params;

		public override string ToString()
		{
			if( template_params == null )
					return name;
			else	return name + '<' + string.Join( ", ", template_params.Select( q => q.ToString() ) ) + '>';
		}
	}

	// vector<int>, array<int,4>
	//        ~~~         ~~~ ~
	// types or integer constants
	public class MyTemplateParam : MyBase
	{
		public string	constant;
		public MyType	type;

		public bool IsType { get { return type != null; } }

		public override string ToString()
		{
			if ( IsType )	return type.ToString();
			else			return constant;
		}
	}
}
