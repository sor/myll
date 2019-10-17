/*
Check der kollisionen von lokalen vars und von 'using' eingeführten vars
*/
#define var

#include <iostream>
using namespace std;

//pow : func	<P>(int)->int	{...}
//vec =	class	<S>				{...}

// current:
//func	pow	<P>(int)->int	{...}
//class	vec	<S>				{...}

//func	<P>(int)->int	pow	{...}
//class	<S>				vec	{...}

var int glob = 0;
var int pow(int) {return 1;}
namespace NB{}
namespace NA {
	using namespace NB;
	int ga;
}
namespace NB {
	using namespace NA;
	int gb;
}
struct SA {
	int ma;
	void fa(int){}
};
struct SB : public SA {
	int mb;
	void fb(int){}
};
class AC {
public:
	void y(){};
	virtual int a(double)const{return 2131;}
			int a(float) const{return 1131;}
};
class BC : public AC {
	//using AC::y;
public:
	int	 glob = 3;
	const static int sglob = 99;
	//int a(int)   const {return  231;}
	int a(float) const {return 1231;}
//	using AC::a;
	int c() const
	{
		using namespace NB;
		::glob = 1;
		int glob = 2;
		cout << glob << endl;
		cout << ::glob << endl;
		cout << BC::glob << endl;
		cout << BC::sglob << endl;
		cout << this->glob << endl << endl;
		cout << sizeof(AC) << endl;
		cout << sizeof(BC) << endl;
		{
			int pow = 88;
			cout << a(9.0) << endl;
		}
		//z = 1;
		//a = 1;
		return 0;
	}
};

int main(){
	const BC a;
	const AC* b = new BC();
	a.c();
	cout << a.AC::a(9.0) << endl;
	cout << b->a(9.0) << endl;
	cout << "-1 < 0  = " << (-1 < 0 ) << endl;
	cout << "-1 < 0u = " << (-1 < 0u) << endl;
	cout << "-1 < 1u = " << (-1 < 1u) << endl;
	static_assert(-1 < 1u);
}

