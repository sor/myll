#include <cstdlib>
#include <cstdint>
#include <iostream>

using namespace std;

//#define CE constexpr
#define CE

class b1 {
public:
	using C = b1;		// the class itself
	using T = uint8_t;	// underlying type

	static const T
		ZERO = 0;

	T data;
		
	CE bool	operator==(const C&) const;
	CE bool	operator!=(const C&) const;
	
};


using C1 = b1;
using T1 = C1::T;

CE bool C1::operator==(const C1& o) const	{ return (data == ZERO) == (o.data == ZERO);	}
CE bool	C1::operator!=(const C1& o) const	{ return (data == ZERO) != (o.data == ZERO);	}


class b8 {
public:
	using C = b8;		// the class itself
	using T = uint8_t;	// underlying type

	static const T
		ZERO = 0,
		MIN = 0,
		MAX = 254,
		NANB = 255;

	T data;

	CE bool is_nan() const;
	CE void set_nan();

	CE 		operator bool() const;
	CE bool operator!() const;
	CE C	operator~() const;

	CE C	operator+(const C&) const;
	CE C	operator*(const C&) const;
	CE C	operator-(const C&) const;
	CE C	operator/(const C&) const;
	CE C	operator^(const C&) const;

	CE C	operator<<(const C&) const;
	CE C	operator>>(const C&) const;

	CE bool operator|(const C&) const;
	CE bool operator&(const C&) const;
/*
	C&	operator++();
	C	operator++(int);
	C&	operator--();
	C	operator--(int);
*/
};

using C = b8;
using T = C::T;

CE bool C::is_nan() const				{ return (*this).data == NANB;	}
CE void C::set_nan()					{ data = NANB;					}

CE 		C::operator bool() const		{ return data != ZERO;							}
CE bool C::operator!() const			{ return data == ZERO; 							}
CE C	C::operator~() const			{ return C{ static_cast<T>(~data)			};	}

CE C	C::operator+(const C& o) const	{ return C{ static_cast<T>(data  | o.data)	};	}	// or,			merge
CE C	C::operator*(const C& o) const	{ return C{ static_cast<T>(data  & o.data)	};	}	// and,			mask
CE C	C::operator-(const C& o) const	{ return C{ static_cast<T>(data  & ~o.data)	};	}	// differenz,	select o
CE C	C::operator/(const C& o) const	{ return C{ static_cast<T>(~data | o.data)	};	}	// implikation
CE C	C::operator^(const C& o) const	{ return C{ static_cast<T>(data  ^ o.data)	};	}	// exor

CE C	C::operator<<(const C& o) const	{ return C{ static_cast<T>(data << o.data)	};	}	// lsh
CE C	C::operator>>(const C& o) const	{ return C{ static_cast<T>(data >> o.data)	};	}	// rsh

CE bool C::operator|(const C& o) const	{ return (data | o.data) != ZERO; }
CE bool C::operator&(const C& o) const	{ return (data != ZERO) & (o.data != ZERO); }
//C	C::operator*(const C& o) const	{ return C{ data & o.data };	}

/*
C&	C::operator++()		{					++data;	return *this;	}
C&	C::operator--()		{					--data;	return *this;	}
C	C::operator++(int)	{ C t = C{ data };	++data;	return t;		}
C	C::operator--(int)	{ C t = C{ data };	--data;	return t;		}
*/
#define ENUM( name, bits )\
	enum class name : uint ## bits ## _t;\
	CE bool operator!(const name & self) { return static_cast<uint ## bits ## _t>(self) != 0; }\
	CE name	operator~(const name & self) { return static_cast<name>(~static_cast<uint ## bits ## _t>(self)); }\
	CE name operator+(const name & l, const name & r) { return static_cast<name>( static_cast<uint ## bits ## _t>(l) |  static_cast<uint ## bits ## _t>(r)); }\
	CE name operator-(const name & l, const name & r) { return static_cast<name>( static_cast<uint ## bits ## _t>(l) & ~static_cast<uint ## bits ## _t>(r)); }\
	CE name operator*(const name & l, const name & r) { return static_cast<name>( static_cast<uint ## bits ## _t>(l) &  static_cast<uint ## bits ## _t>(r)); }\
	CE name operator/(const name & l, const name & r) { return static_cast<name>(~static_cast<uint ## bits ## _t>(l) |  static_cast<uint ## bits ## _t>(r)); }\
	CE name operator^(const name & l, const name & r) { return static_cast<name>( static_cast<uint ## bits ## _t>(l) ^  static_cast<uint ## bits ## _t>(r)); }\
	CE name& operator+=(name & l, const name & r) { return l = l + r; }\
	CE name& operator-=(name & l, const name & r) { return l = l - r; }\
	CE name& operator*=(name & l, const name & r) { return l = l * r; }\
	CE name& operator/=(name & l, const name & r) { return l = l / r; }\
	CE name& operator^=(name & l, const name & r) { return l = l ^ r; }\
	enum class name : uint ## bits ## _t

ENUM( tf, 8 )
{
    EFlagsNone  = 0,
    EFlagOne    = (1 << 0),
    EFlagTwo    = (1 << 1),
    EFlagThree  = (1 << 2),
    EFlagFour   = (1 << 3)
};

#define SIZE 1024*1024+256

int main()
{
	tf f = tf::EFlagOne;
	b8 a [SIZE];
	b8 b [SIZE];
	b8 c [SIZE];

	for( unsigned i = 0; i < SIZE; ++i )
	{
		a[i].data = rand();
		b[i].data = rand();
	}

	for( unsigned j = 0; j < 512; ++j )
	{
		if( rand() > 666 )
			f = f + tf::EFlagTwo;
		const T jj = j/128;
		for( unsigned i = 0; i < SIZE; ++i )
//			c[i].data = (a[i] | b[i] ? 1 : 0) + jj;
			c[i].data = ((a[i] << C{3}) * b[i]).data + jj + static_cast<uint8_t>(f);
	}
	cout << rand() << endl;
	cout << sizeof( int8_t ) << sizeof( int16_t ) << sizeof( int32_t ) << sizeof( int64_t ) << endl;
	cout << sizeof( int_fast8_t ) << sizeof( int_fast16_t ) << sizeof( int_fast32_t ) << sizeof( int_fast64_t ) << endl;
	bool test = true;
	int t = 8;
	cout << test << endl;
	cout << test << (*((bool*)(void*)(&t))) << endl;
	return c[SIZE-1].data;
}

static_assert( sizeof( C ) == sizeof( T ), "Size Wrong" );

