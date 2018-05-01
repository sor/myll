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
MyLang		-> C++
int[16]		-> array<int,16>
int[*]		-> int *	// pointer that can hold an array
int*		-> int *	// pointer to a single element
int[]		-> vector<int>
int[](16)	-> vector<int>(16)
int[init:16]-> vector<int>(16)

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
where	T has_const_iterator & has_begin_end,
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
	return ret;
}
