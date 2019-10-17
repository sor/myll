#include <iostream>

class i8 {
public:
	using C = i8;
	using T = signed char;

	static const T
		MIN = -127,
		MAX = 127,
		NAN = -128;

	T data;

	bool is_nan() const		{	return data == NAN;	}

	C&	operator++()		{						++data;	return *this;	}
	C	operator++( int )	{ i8 tmp = i8{ data };	++data;	return tmp;		}
};

using namespace std;

unsigned int uitoui( unsigned int i ) 	{	return i;}
unsigned int ustoui( unsigned short s ) {	return s;}
unsigned int uctoui( unsigned char c ) 	{	return c;}

int uitoi( unsigned int i ) {	return i;}
int ustoi( unsigned short s ) {	return s;}
int uctoi( unsigned char c ) {	return c;}

int itoi( int i ) {	return i;}
int stoi( short s ) {	return s;}
int ctoi( char c ) {	return c;}

unsigned int itoui( int i ) {	return i;}
unsigned int stoui( short s ) {	return s;}
unsigned int ctoui( char c ) {	return c;}

int main()
{
	i8 ii;
	ii.is_nan();
	ii++;
	++ii;
	int i = 8;
	unsigned int ui = 9;
	short s = 10;
	unsigned short us = 11;
	char c = 12;
	unsigned char uc = 13;
	ui = ui;
	ui = us;
	ui = uc;
	ui = i;
	ui = s;
	ui = c;
	i = ui;
	i = us;
	i = uc;
	i = i;
	i = s;
	i = c;
	ui = uitoui( ui );
	ui = ustoui( us );
	ui = uctoui( uc );
	//cout << "Hello Mint!" << endl;
	return 0;
}

