#include "variadic.hpp"
int Builder::count = 0;
int main()
{
    std::string expect = "1|2.5";
    if( forwardChain<int, float>( 1, 2.5f ) != expect ) {
        return 1;
    }
    if( relayEmpty<>() != "empty" ) {
        return 2;
    }
    if( takeOne<int>( 5 ) != 5 ) {
        return 3;
    }
    logMixed<int, float, float>( 7, 1.5f, 2.5f );
    Builder b( 1, 2.5f );
    if( Builder::count != 3 ) {
        return 4;
    }
    Builder nothing{};
    if( Builder::count != 3 ) {
        return 5;
    }
    return 0;
}
