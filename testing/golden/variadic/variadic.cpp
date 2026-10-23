#include "variadic.hpp"
int Builder::count = 0;
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
    if( forwardChain<int, float>( 1, 2.5f ) != expect ) {
        return 3;
    }
    if( relayEmpty<>() != "empty" ) {
        return 4;
    }
    if( takeOne<int>( 5 ) != 5 ) {
        return 5;
    }
    logMixed<int, float, float>( 7, 1.5f, 2.5f );
    Builder b( 1, 2.5f );
    if( Builder::count != 3 ) {
        return 6;
    }
    Builder nothing{};
    if( Builder::count != 3 ) {
        return 7;
    }
    return 0;
}
