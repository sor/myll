#include "convert_test.hpp"
A::A(int other)
{
    i = other;
}
A::operator bool() const
{
    return i > 0;
}
B::B(int other)
{
    i = other;
}
C::C(bool b)
{
    i = b ? 1 : 2;
}
D::D(int other)
{
    i = other;
}
D::operator bool() const
{
    return i > 0;
}
E::E(int other)
{
    i = other;
}
E::operator bool() const
{
    return i > 0;
}
F::F(int other)
{
    i = other;
}
F::operator bool() const
{
    return i > 0;
}
int run_convert_decl()
{
    A a = 5;
    if( !a ) {
        return 1;
    }
    A b = -3;
    if( b ) {
        return 2;
    }
    B c = 7;
    if( c.i != 7 ) {
        return 3;
    }
    C d = true;
    if( d.i != 1 ) {
        return 4;
    }
    D f = 4;
    if( !f ) {
        return 5;
    }
    E e = 9;
    if( !e ) {
        return 6;
    }
    F g = F( 7 );
    if( g.i != 7 ) {
        return 7;
    }
    F h = static_cast<F>( 8 );
    if( h.i != 8 ) {
        return 8;
    }
    F k( 9 );
    if( k.i != 9 ) {
        return 9;
    }
    if( !(static_cast<bool>( g )) ) {
        return 10;
    }
    return 0;
}
bool takesString(const std::string& s)
{
    return s == "hello";
}
int run_convert_string()
{
    const std::string s = "hello";
    if( s != "hello" ) {
        return 1;
    }
    if( !takesString( "hello" ) ) {
        return 2;
    }
    return 0;
}
int main()
{
    if( run_convert_decl() != 0 ) {
        return 1;
    }
    if( run_convert_string() != 0 ) {
        return 2;
    }
    return 0;
}
