std::array<int,4> a;				int@[4] a;
std::array<int,4> a[4];				int@[4][4] a;
std::array<std::array<int,2>,2> a = {{{8, 2}, {5, 3}}};
									int@[2,2] = {{8, 2}, {5, 3}};
std::array<std::array<std::array<int,2>,2>,2> a = {{{{{5, 8}, {8, 3}}}, {{{5, 3}, {4, 9}}}}};
									int@[2,2,2] = {{{5, 8}, {8, 3}}, {{5, 3}, {4, 9}}};

std::vector<int> v;					int@[] v;
std::vector<int> v { 1,2,3 };		int@[] v { 1,2,3 };

std::unordered_set<data_type> s;
data_type{} s;

std::unordered_map<index_type,data_type> m;
data_type{index_type} m;

//////////////////////////// BESSER:
MyLang			-> C++
int*			-> int *	// pointer to a single element
int[*]			-> int *	// pointer that can hold an array
int[16]			-> array<int,16>
int[]			-> vector<int>
int[](16)		-> vector<int>(16)
int[](16,99)	-> vector<int>(16,99)
int[init:16]	-> vector<int>(16)
int[reserve:16]	-> vector<int>; v.reserve(16)
int[>16]		-> vector<int>; v.reserve(16)
int[16+]		-> vector<int>; v.reserve(16)

// TODO: find a way to make arrays that can only be indexed by specified enums

// compatibility, this from C/C++ is possible
int@[]		-> int[];
int@[16]	-> int[16];

int *!		-> unique_ptr<int>
int *+		-> shared_ptr<int>
int *?		-> weak_ptr<int>

string[int]				-> unordered_map<int,string>
string[int,hasher](16)	-> unordered_map<int,string,hasher<int>>(16)
string[int,<]			-> map<int,string,less<int>>
string[int,>]			-> map<int,string,greater<int>>
string[int,+](16)		-> unordered_multimap<int,string>(16)
string[int,<+](16)		-> multimap<int,string>(16)

string{}			-> unordered_set<string>
string{}(16)		-> unordered_set<string>(16)
string{<}			-> set<string>
string{>}			-> set<string,greater>

string[set]			-> unordered_set<string>
string[set,16]		-> unordered_set<string> // reserve 16 if possible
string[set,<]		-> set<string>
string[set,>]		-> set<string,greater>
string[<set]		-> set<string>
string[>set]		-> set<string,greater>
string[<+set]		-> set<string>
string[>set]		-> set<string,greater>

string[deque]		-> deque<string>
string[heap]		-> priority_queue<string>
string[list]		-> list<string>
string[stack]		-> stack<string>
etc

pair<string,int>[map]
string[map<int>]
string[set]


class Base<T>
where T : traits ( has_const_iterator, has_begin_end )

class Base<T,V>
where	T has_const_iterator && has_begin_end,
		V is_class
requires
	is_class<V>,
	has_const_iterator<T>,
	has_begin_end<T>
{
	int i;
}

int *! uniq;
int a = *uniq + 6;


func blah() -> produce BigOne
{
	ret = new();
	// or
	ret = new BigOne();
}

// wird zu

unique_ptr<BigOne> blah()
{
	unique_ptr<BigOne> ret = new unique_ptr<BigOne>();
	return std::move(ret);
}

// attributes

linkage = reachability (not accessability)
[global,extern,external] var int i;	// extern int i;
[hidden,intern,internal] var int j;	// static int j;

[virtual(forbidden,discouraged,encouraged,enforced), pod == (virtual=forbidden)]
class X {
	[abstract,virtual,override,final,hide]
	func a();
}

[conversion(auto,default,implicit,explicit)]
ctor( OTHER other) {...}

string[int]				-> unordered_map<int,string>
string[int,hasher](16)	-> unordered_map<int,string,hasher<int>>(16)
string[int,<]			-> map<int,string,less<int>>
string[int,>]			-> map<int,string,greater<int>>
string[int,+](16)		-> unordered_multimap<int,string>(16)
string[int,<+](16)		-> multimap<int,string>(16)

string{}			-> unordered_set<string>
string{}(16)		-> unordered_set<string>(16)
string{<}			-> set<string>
string{>}			-> set<string,greater>

string[set]			-> unordered_set<string>
string[set,16]		-> unordered_set<string> // reserve 16 is possible
string[set,<]		-> set<string>
string[set,>]		-> set<string,greater>
string[<set]		-> set<string>
string[>set]		-> set<string,greater>
string[<+set]		-> set<string>
string[>set]		-> set<string,greater>

string[deque]		-> deque<string>
string[heap]		-> priority_queue<string>
string[list]		-> list<string>
string[stack]		-> stack<string>

[ // rule file myll::magic
magic_return_val=[ret,result],
magic_param_val=other,
magic_param_ref=other,
magic_param_ptr=that,
magic_uscore=true,
magic_autoindex=true,

convert_decl=true,
default_on_semicolon=true,
support_nullptr=warning,
class_default=[priv,rule_of_n],
struct_default=[pub,pod],
method_default=[instance],
func_default=[global,pure],
proc_default=[global],

unique_pointer=std::unique_ptr<T>,
shared_pointer=std::shared_ptr<T>,
weak_pointer=std::weak_ptr<T>,

// static_array: T@[16], dynamic_array: T@[],
static_array=std::array<T,S>,
dynamic_array=std::vector<T>,
ordered_dict=std::map<K,V>, // sorted_dict
unordered_dict=std::unordered_map<K,V>,
ordered_set=std::set<T>,
unordered_set=std::unordered_set<T>,
ordered_multidict=std::multimap<K,V>,
unordered_multidict=std::unordered_multimap<K,V>,
ordered_multiset=std::multiset<T>,
unordered_multiset=std::unordered_multiset<T>,

static_cast=static_cast<T>,
dynamic_cast=dynamic_cast<T>,
const_cast=const_cast<T>,
reinterpret_cast=reinterpret_cast<T>,
bit_cast=std::bit_cast<T>,
narrowing_conversion=false, // also set related C++ compiler warnings as errors to be sure
]
class rule::myll::magic {}

[ // rule file myll::retro
magic_return_val=[],
magic_param_val=[],
magic_param_ref=[],
magic_param_ptr=[],
magic_uscore=false,
magic_autoindex=false,
convert_decl=false,
default_on_semicolon=false,
support_nullptr=true,
class_default=[priv],
struct_default=[pub],
method_default=[],
func_default=[],
proc_default=[],
unique_pointer=std::unique_ptr<T>,
shared_pointer=std::shared_ptr<T>,
weak_pointer=std::weak_ptr<T>,
static_array=false,
dynamic_array=false,
static_cast=static_cast<T>,
dynamic_cast=dynamic_cast<T>,
const_cast=const_cast<T>,
reinterpret_cast=reinterpret_cast<T>,
bit_cast=std::bit_cast<T>,
narrowing_conversion=true,
]
class rule::myll::retro {}

[ // rule file myll::madness
unique_pointer=T*,
shared_pointer=T*,
weak_pointer=T*,
static_cast=((T)E),
dynamic_cast=((T)E),
const_cast=((T)E),
reinterpret_cast=((T)E),
bit_cast=((T)E),
]
class rule::myll::madness {}


[virtual=encouraged]
class Test
{
	field int[*] data;
	field int size, capacity;

	[dispatch=static, nonvirtual]
	func
	{
		[dispatch=dynamic, virtual]
		top() => data[size ? size-1 : 0];

		pop() => size && --size;
	}

	func push( int val )
	{
		//if(size+1 < capacity)
		data[size] = val;
		++size;
	}

	[virtual=inherit]
	class Sub {}
}

top
	[dispatch=dynamic, virtual]		// sets to virtual
	[dispatch=static, nonvirtual]	// applies afterwards, notices overlap and discards
	[virtual=encouraged]			// applies afterwards, notices overlap and does not disagree

pop
	[dispatch=static, nonvirtual]	// sets to nonvirtual
	[virtual=encouraged]			// applies afterwards, notices overlap and does not disagree

push
	[virtual=encouraged]			// notices no direct setting, applies virtual

different idea
top
	[virtual=encouraged] on class saves that child funcs by default should be virtual
	[dispatch=static, nonvirtual] on func, receives attr from class 	// applies afterwards, notices overlap and discards
	[dispatch=dynamic, virtual]		// sets to virtual


shortcut table
[final]		class	=>	[dispatch=disallow]	if no virtual base class
					||	[dispatch=final]	if virtual base class
[pod]		class	=>	[dispatch=disallow]	=> applies [dispatch=static]	func, among other things
[interface]	class	=>	[dispatch=interface]=> applies [dispatch=abstract]	func
[nonvirtual]class	=>	[dispatch=forbid]
[virtual]	class	=>	[dispatch=basic]	=> applies [virtual] dtor
[static]	class	=>	[storage=onlystatic]
[instance]	class	=>	[storage=onlyinstance]

[nonvirtual]func	=>	[dispatch=nonvirtual]
[abstract]	func	=>	[dispatch=virtual, impl=abstract]
[virtual]	func	=>	[dispatch=virtual]
[override]	func	=>	[dispatch=override]
[final]		func	=>	[dispatch=final]

[manual]	enum	=>	[startcount=nil,counting=manual]
[]			enum	=>	[startcount=0,	counting=increment]
[enum]		enum	=>	[startcount=0,	counting=increment]
[flags]		enum	=>	[startcount=1,	counting=doubling]
[enum]		enumval	=>	[startcount=???,counting=increment]
[flags]		enumval	=>	[startcount=???,counting=doubling]

[global]	var		=>	[storage=static]

Dr K sprach grad über seinen Guru, der sowas ist wie ein Coach...
und dann dachte ich mir ich mache sowas bei unseren neuen Codern,
vielleicht sollte ich das auch freiberufich machen.

Error ausgeben wenn eine nicht virtuelle klasse implizit zur basisklasse gecastet wird


//h
extern int j;			// visibility external,
const  int k;			// visibility internal
extern const int ek;	// visibility external, neet to be extern in h and cpp
extern const int j;		// visibility external,
class Blah {
	static int i;		// storage duration from start to end of program
	int f() {			// this member function is dispatched statically
		static int i;	// storage duration from first call to end of program
		int j = i;		// statically checked that the types are assign-compatible
	}
};
//cpp
static int i;			// visibility internal, other TU can not see this, maybe not in header?
int j;
int Blah::i = 0;

// var in global or NS need to specify one of [module], [global,extenal], [hidden,internal]
// DO NOT rely on linkage internal/hidden, since merged cpp building might copy multiple files together
// so a cpp file might have two [internal] var int x; and then fail to compile
// module linkage can fix this, so rather only use [global] and [module]
[linkage=module]				var		int mv;	// visibility internal, but since we have merged module builds...
[linkage=module]				const	int mc;	// visibility internal, ... (fusion of all cpp) this might work
[hidden,intern(al),NOT static]	var		int hv;	// visibility internal, other TU can not see this
[hidden,intern(al),NOT static]	const	int hc;	// visibility internal, other TU can not see this
[global,extern(al),visible]		var		int gv;	// visibility external / global
[global,extern(al),visible]		const	int gc;	// visibility external / global
// NOTE: same for functions
// unnamed NS provide internal linkage
class Blah {
	[static,shared,persist(ant)]
	var int i = 0;	// storage duration from start to end of program
	func f -> int {			// this member function is dispatched statically
		[keep,once,static,persistant,prevail,retain]
		var int i;		// storage duration from first call to end of program
	}
};
int Blah::i = 0;

4 is not the same as +4
	+4 could be of some type like offset-int, allowing ctors to make a different
		overload based on this.
	e.g. Vec<int>(+4) allocates 4 slots more than the default.
	+-4 could be the same for negative numbers (-4 could already be, if the ctor
		handles it differently, but still the original type)

how to do explicit template instantiation?

Removed in Myll compared to C and C++
(functionality which was only renamed, is not considered removed)
- preprocessor (instead use attributes, constexpr)
- the split between implementation and header files (.cpp/.h/.hpp/...)
- operator ","
- operator prefix "+" ???
- typedef (use "using" instead)
- non-class enums (use "class enum" instead)
- directly declare an object of a newly declared type (struct B{} b;)
- local classes, structs, enums, and functions inside functions. (lambdas can replace local functions)
- forward declarations (import what you need from other modules)
- assignment in expressions
- throw in expressions
- goto
- unsigned long long and a lot of similar typenames with spaces are gone
- using {} for initialization everywhere (it solves a problem in C++ which Myll does not have)

Added:
- Opt in to convention over configuration (your own convention via rulesets)
- Automatic checks that your code style and conventions are upheld
- Extensibility via your own attributes (later, this does not change the language per se)


My naming of things:
	variable -> object
	template function -> function template
	type: levels of indirection are part of the type


struct B {
	float x,y,z;
};

void compute_with_B( B * b ) {
	...
}

B b;
compute_with_B(b);

// top solution is exactly the same as bottom
// you can only use do_with_B with a object B
// same as with the solution below

class B {
	float x,y,z;
	void compute() {
		...
	}
};

B b;
b.compute();

// Why do I like namespaces and object bound functions?
SDL_Renderer render;
SDL_RenderDrawLine(render, ...);

// If I analyzed that I have no collision with the name Renderer then I could
// globally add "using Renderer = SDL::Renderer;"
// or if I'm crazy even "using namespace SDL;"
SDL::Renderer render;
render.DrawLine(...); // working autocompletion and 10 significant chars less to write each line

// The code has the same performance as it produces the same binary,
// assuming that you did not intentionally sabotage DrawLine by making it unnecesarily virtual.
// This is not about polymorphism.

