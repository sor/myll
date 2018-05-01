using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLang.Core
{
	public class Qualifier : IBase {
		[Flags]
		public enum Type {
			None		= 0,
			Const		= 1,
			Volatile	= 2,
			Mutable		= 4, // Anti-Const?
		}
		public Qualifier.Type type = Type.None;

		public override string ToString() {
			if ( type == Qualifier.Type.None )
				return string.Empty;
			
			List<string> ret = new List<string>( 3 );
			if ( type.HasFlag( Qualifier.Type.Const ) )		ret.Add( "const" );
			if ( type.HasFlag( Qualifier.Type.Volatile ) )	ret.Add( "volatile" );
			if ( type.HasFlag( Qualifier.Type.Mutable ) )	ret.Add( "mutable" );
			return string.Join( " ", ret );
		}
	}

	public class Pointer : IBase {
		public enum Type {
			RawPtr,
			RawPtrToArray,
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

		#region codegen, migrate out of here
		readonly Dictionary<Type,string> template = new Dictionary<Type, string>
		{
			{ Type.RawPtr,			"{0} * {1}" },
			{ Type.RawPtrToArray,	"{0} * {1}" },
			{ Type.LVRef,			"{0} & {1}" },
			{ Type.RVRef,			"{0} && {1}" },
			{ Type.RawArray,		"{0}[{2}] {1}" },		// TODO: the "name"  must be embedded
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
		#endregion

		public Pointer.Type	type;
		public Qualifier	qualifier = new Qualifier() { type = Qualifier.Type.None };
		public MyExpression	content;

		public MyType		sub_type; // TODO: noch unbenutzt

		public string ToString( string inner ) {
			if		( content  != null )	return string.Format( template[type], inner, qualifier.ToString(), content ).TrimEnd( ' ' );
			else if	( sub_type != null )	return string.Format( template[type], inner, qualifier.ToString(), sub_type ).TrimEnd( ' ' );
			else							return string.Format( template[type], inner, qualifier.ToString() ).TrimEnd( ' ' );
		}
	}

	public sealed class Type : IBase, Library.IResolvable {
		public Qualifier			qualifier	= new Qualifier() { type = Qualifier.Type.None };
		public Specifier			specifier;
		public IEnumerable<Pointer>	pointers	= Enumerable.Empty<Pointer>();

		public bool IsResolved	{ get { return specifier.IsResolved; } }
		public void Unresolve()	{ specifier.Unresolve(); }
		public int  Resolve()	{ return specifier.Resolve(); }

		public override string ToString() {
			var ret = ( qualifier.ToString() + " "
					  + specifier.ToString()
					  ).TrimStart( ' ' );

			foreach( var p in pointers )
				ret = p.ToString( ret );
	
			return ret;
		}
	}

	abstract public class Specifier : IBase, Library.IResolvable {
		public abstract bool IsResolved { get; }
		public abstract void Unresolve();
		public abstract int  Resolve();
	}

	class SimpleSpecifier : Specifier {
		public enum Type {
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
		public enum Signedness {
			DontCare,
			Signed,
			Unsigned
		}

		public SimpleSpecifier.Type			type;
		public SimpleSpecifier.Signedness	sign = Signedness.DontCare;

		public SimpleSpecifier() {}
		public SimpleSpecifier( SimpleSpecifier.Type type ) {
			this.type = type;
		}

		// implicit conversion from Type to SimpleSpecifier
		public static implicit operator SimpleSpecifier( SimpleSpecifier.Type type ) {
			return new SimpleSpecifier( type );
		}

		public override bool IsResolved { get { return true; } }
		public override void Unresolve() { }
		public override int  Resolve() { return 0; }

		public override string ToString() {
			string sgn;

			if( sign != Signedness.DontCare )	sgn = sign.ToString().ToLower() + " ";
			else								sgn = string.Empty;

			switch( type )
			{
				case SimpleSpecifier.Type.LongDouble:	return sgn + "long double";
				case SimpleSpecifier.Type.LongLong:		return sgn + "long long";
				default:								return sgn + type.ToString().ToLower();
			}
		}
	}

	class AdvancedSpecifier : Specifier {
		public List<NestedName>		nested_names = new List<NestedName>();

		public AdvancedSpecifier() { }

		public int			 unresolved = -1;
		public override bool IsResolved { get { return unresolved == 0; } }
		public override void Unresolve() { unresolved = -1; }
		public override int  Resolve() {
			if( IsResolved )
				return 0;
			return unresolved = nested_names.Select( q => q.Resolve() ).Sum();
		}

		public override string ToString() {
			return string.Join( "::", nested_names.Select( q => q.ToString() ) );
		}
	}

	// nested name specifier, gesamtheit der MySegments
	public class NestedName : IBase, Library.IResolvable {
		public string				name;
		public List<TemplateArg>	template_args = new List<TemplateArg>();

		public int	unresolved = -1;
		public bool	IsResolved { get { return unresolved == 0; } }
		public void	Unresolve() { unresolved = -1; }
		public int	Resolve() {
			if( IsResolved )
				return 0;
			return unresolved = template_args.Select( q => q.Resolve() ).Sum();
		}

		public override string ToString() {
			if( template_args.Count == 0 )
				return name;

			return name + "<" + string.Join( ",", template_args.Select( q => q.ToString() ) ) + ">";
		}
	}

	abstract public class TemplateArg : IBase, Library.IResolvable {
		public abstract bool IsResolved { get; }
		public abstract void Unresolve();
		public abstract int  Resolve();
	}

	public class TemplateArgType : TemplateArg {
		public Type type;

		public override bool IsResolved { get { return type.IsResolved; } }
		public override void Unresolve() { type.Unresolve(); }
		public override int  Resolve() {
			if( IsResolved )
				return 0;

			return type.Resolve();
		}

		public override string ToString() { return type.ToString(); }
	}

	public class TemplateArgLiteral : TemplateArg {
		public MyIntegerLiteral lit;

		public override bool IsResolved { get { return true; } }
		public override void Unresolve() { }
		public override int  Resolve() { return 0; }

		public override string ToString() { return lit.ToString(); }
	}

	public class TemplateArgTemplateParam : TemplateArg {
		public override bool IsResolved { get { return true; } }
		public override void Unresolve() { }
		public override int  Resolve() { return 0; }

		public override string ToString() { return "TODO".ToString(); }
	}


	abstract public class MyType : IBase {
		public Qualifier			qualifier	= new Qualifier() { type = Qualifier.Type.None };
		public IEnumerable<Pointer>	ptr			= Enumerable.Empty<Pointer>();

		public override string ToString() {
			var ret = (qualifier.ToString() + " " + SubGen()).TrimStart( ' ' );
			foreach( var p in ptr )
			{
				ret = p.ToString( ret );
			}
			return ret;
		}

		protected abstract string SubGen();
	}

	public class MyBasicType : MyType {
		public enum Type {
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

		public enum Signedness {
			DontCare,
			Signed,
			Unsigned
		}

		public Signedness	sign = Signedness.DontCare;
		public Type			type;

		public MyBasicType() {}
		public MyBasicType( MyBasicType.Type type ) {
			this.type = type;
		}

		protected override string SubGen() {
			if ( sign == Signedness.DontCare )	return						type.Gen();
			else								return sign.Gen() + " " +	type.Gen();
		}
	}

	// ns::ns<...>::name<tpl,tpl>
	public class MyAdvancedType : MyType {
		public IEnumerable<MySegment>	segments;

		protected override string SubGen() {
			return string.Join( "::", segments.Select( q => q.ToString() ) );
		}

		public bool Resolve() {
			/* TODO: hier weiter?
			Backend.MyHierarchic	m;
			MySegment				s = segments.First();

			// im der aktuellen hierarchiestufe nach diesem namen suchen
			// in der darüberliegenden, immer weiter hoch
			// bis zu global
			// passiert alles in ContainsTypeTree
			Backend.MyNamed			n = m.ContainsTypeTree( s.name );
			if( n == default( Backend.MyNamed ) )
				return true;

			// dann die "using namespace xxx" einträge abklappern
			IEnumerable<Backend.MyHierarchic> u_nss;
			Backend.MyHierarchic h = Backend.MyGlobal.instance;
			foreach( var u_ns in u_nss )
			{
				u_ns.ContainsType( s.name );
				//h.ContainsType( u_ns )
			}
			*/
			return true;
		}
	}

	// nested name specifier, gesamtheit der MySegments
	public class MySegment : IBase {
		public string			name;
		public MyTemplateArgs	template_params;

		public MySegment( string name ) {
			this.name = name;
		}
		public override string ToString() {
			if( template_params == null )	return name;
			else							return name + '<' + template_params.ToString() + '>';
		}
	}

	// vector<int>, array<int,4,T>
	//        ~~~         ~~~ ~ ~
	// types or integer constants or surrounding template parameters
	abstract public class MyTemplateArg : IBase {
		/*
		public string	constant;
		public MyType	type;

		public bool IsType { get { return type != null; } }

		public override string ToString() {
			if( IsType )	return type.ToString();
			else			return constant;
		}
		*/
	}

	public class MyTemplateArgType : MyTemplateArg {
		public MyType type;
		public override string ToString() { return type.ToString(); }
	}

	public class MyTemplateArgLiteral : MyTemplateArg {
		public MyIntegerLiteral lit;
		public override string ToString() { return lit.ToString(); }
	}

	public class MyTemplateArgTemplateParam : MyTemplateArg {
		public override string ToString() { return "TODO".ToString(); }
	}
}
