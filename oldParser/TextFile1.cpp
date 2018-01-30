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

(ordered)set		-> {<}
unordered_set		-> {}
(ordered)multiset	-> {<+},{+<}
unordered_multiset	-> {+}


