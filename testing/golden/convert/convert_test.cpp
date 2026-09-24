#include "convert_test.hpp"
A::A(int other)
{
    i = other;
}
A::operator bool() const
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
