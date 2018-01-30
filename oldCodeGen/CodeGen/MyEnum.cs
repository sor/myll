using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanSordid.MyLang.CodeGen
{
	class MyEnum
	{
		#region VisibleDeclaration Template Strings
		static readonly string declaration_template_entry_pair = "\t\t\t\t{0}\t= {1},\n";
		static readonly string declaration_template_entry_solo = "\t\t\t\t{0},\n";

		// 0 = name, 1 = list, 2 = count, 3 = min, 4 = max, 5 = name with ns, 6 = start ns, 7 = end ns
		static readonly string declaration_template = @"
			{6}
			enum class {0}
			{{
				{1}
			}};
			ENUM_CLASS_BITWISE( {5} );
			{7}

			#pragma region {5} additions: numeric_limits: min, max, count, to_string and ostream::operator<<
			//{{            {5} additions: numeric_limits: min, max, count, to_string and ostream::operator<<
				namespace std
				{{
					template<>
					class numeric_limits<{5}>
						: public numeric_limits<underlying_type<{5}>::type>
					{{
					public:
						static constexpr {5} min() noexcept {{ return {5}::{3}; }}
						static constexpr {5} max() noexcept {{ return {5}::{4}; }}
						static const_maybe_expr size_t count = {2};
					}};

					// inline in the future somehow
					string
					to_string( const {5} e );
				}}

				#ifdef _OSTREAM_
				inline std::ostream &
				operator<<( std::ostream & o, const {5} e )
				{{
					o << std::to_string(e);
					return o;
				}}
				#endif // _OSTREAM_

			//}} {5} additions
			#pragma endregion";
		#endregion

		#region ConcreteImplementation Template Strings
		// 0 = name, 1 = key, 2 = value
		static readonly string concrete_template_map_emplace	=	@"m.emplace({0}::{1},	""{1}({2})"" );";
		// 0 = name, 1 = key, 2 = value
		static readonly string concrete_template_map_initlist	=			@"{{ {0}::{1},	""{1}({2})"" }},";
		// 0 = key, 1 = value
		static readonly string concrete_template_array_entry	=						  @"""{0}({1})"", ";

		// 0 = name, 1 = n-map_initlist, 2 = ns_name
		static readonly string concrete_template_map = @"
			/*anonymous*/ namespace
			{{	
				// if numbered by hand
				const
				std::map<const {0}, const std::string>
				{2}_NameContainer = {{
					{1}
				}};
			}}

			// Would like this to be UFCS callable, maybe later its possible
			std::string
			std::to_string( const {0} e )
			{{
				if(	{2}_NameContainer.find(e) != {2}_NameContainer.end() )
					return {2}_NameContainer.at(e);
				else
					return ""OUT OF RANGE("" + to_string( base_cast(e) ) + "")"";
			}}";

		// 0 = name with ns, 1 = n-array_entry, 2 = ns_name
		static readonly string concrete_template_array = @"
			/*anonymous*/ namespace
			{{
				// if numbered automatically
				const
				std::array<const std::string, std::numeric_limits<{0}>::count>
				{2}_NameContainer = {{ {1} }};
			}}

			// Would like this to be UFCS callable, maybe later its possible
			std::string
			std::to_string( const {0} e )
			{{
				const auto e_base = base_cast(e);
				if (e_base < std::numeric_limits<{0}>::count)
					return {2}_NameContainer.at(e_base);
				else
					return ""OUT OF RANGE("" + to_string( e_base ) + "")"";
			}}";
		#endregion

		public static readonly HashSet<string> NeedCppClasses = new HashSet<string> {
			"std::numeric_limits",
			"std::string",
			"std::underlying_type",
		};

		// ------------------------------------------------------------------
		// fields

		public MyNamespace	ns;
		public string		name;
		public List<KeyValuePair<string,string>> entries;

		private bool		AutoIndexed;
		private bool		ManualIndexed;

		// only for codegen
		private int			LongestKey;

		// ------------------------------------------------------------------
		// properties

		public HashSet<string> DynNeedCppClasses
		{
			get
			{
				return new HashSet<string> {
					this.AutoIndexed
						? "std::array"
						: "std::map" };
			}
		}

		// ------------------------------------------------------------------
		// constructor

		public MyEnum( MyNamespace ns, string name )
		{
			this.ns				= ns;
			this.name			= name;
			this.entries		= new List<KeyValuePair<string, string>>();
			this.AutoIndexed	= true;
			this.ManualIndexed	= true;
			this.LongestKey		= 0;
		}

		// ------------------------------------------------------------------
		// methods

		// value is string since you can pass more than just numbers
		public void Add( string k, string v )
		{
			entries.Add( new KeyValuePair<string, string>( k, v ) );
			LongestKey = Math.Max( LongestKey, k.Length );
			AutoIndexed = false;
		}

		public void Add( string k )
		{
			entries.Add( new KeyValuePair<string, string>( k, null ) );
			LongestKey = Math.Max( LongestKey, k.Length );
			ManualIndexed = false;
		}

		public string VisibleDeclaration()
		{
			List<string> list = new List<string>( entries.Count );
			foreach( var e in entries )
			{
				if( e.Value != null )
					list.Add( declaration_template_entry_pair.Fmt( e.Key, e.Value ) );
				else
					list.Add( declaration_template_entry_solo.Fmt( e.Key ) );
			}

			return declaration_template.Fmt(
						name,
						list.Join().TrimStart( '\t' ).TrimEnd( '\n' ).TrimEnd( ',' ),
						entries.Count.ToString(),
						entries.First().Key,
						entries.Last().Key,
						ns.Name + name,
						ns.StartNS,
						ns.EndNS
					).Replace( "\t", "  " );
		}

		public string ConcreteImplementation()
		{
			string map_or_array;

			string full_name		= ns.Name + name;
			string full_underscored	= full_name.Replace( "::", "_" );

			if( AutoIndexed )
			{
				int i = 0;
				List<string> list = new List<string>( entries.Count );
				foreach( var e in entries )
				{
					list.Add( concrete_template_array_entry.Fmt( e.Key, i ) );
					i++;
				}
				map_or_array = concrete_template_array.Fmt(
									full_name,
									list.Join().TrimStart( '\t' ).TrimEnd( '\n' ).TrimEnd( ',' ),
									full_underscored
								);
			}
			else if( ManualIndexed )
			{
				List<string> list_init = new List<string>( entries.Count );
				foreach( var e in entries )
				{
					list_init.Add( concrete_template_map_initlist.Fmt( full_name, e.Key, e.Value ) );
				}

				map_or_array = concrete_template_map.Fmt(	full_name,
															list_init.Join( "\n\t\t\t\t\t" ),
															full_underscored
														);
			}
			else
			{
				// Some values are given, others are not
				throw new NotImplementedException();
			}


			return map_or_array.Replace( "\t", "  " );
		}
	}
}
