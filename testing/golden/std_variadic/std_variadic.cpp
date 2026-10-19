#include "std_variadic.hpp"
int main()
{
    std::string s = std::format( "{}|{}", 1, 2.5f );
    std::string expect = "1|2.5";
    if( s != expect ) {
        return 1;
    }
    std::println( "values: {} and {}", 1, 2.5f );
    std::string one = std::format( "just {}", 7 );
    if( one != "just 7" ) {
        return 2;
    }
    return 0;
}
