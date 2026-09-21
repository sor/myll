#include "operator_defaults.hpp"
Num::Num(int value)
{
    v = value;
}
Num Num::operator+(const Num& other) const
{
    return Num( v + other.v );
}
Num& Num::operator+=(const Num& other)
{
    v = v + other.v;
    return (*this);
}
Num& Num::operator++()
{
    v = v + 1;
    return (*this);
}
int main()
{
    Num a = Num( 1 );
    Num b = Num( 2 );
    const Num& ca = a;
    Num c = ca + b;
    if( c.v != 3 ) {
        return 1;
    }
    a += b;
    if( a.v != 3 ) {
        return 2;
    }
    ++a;
    if( a.v != 4 ) {
        return 3;
    }
    return 0;
}
