#include "external_algorithms_test.hpp"
int main()
{
    App::Simple<int> s{};
    s.pushBack( 3 ).pushBack( 1 ).pushBack( 4 ).pushBack( 1 ).pushBack( 5 );
    s.sort();
    if( s.at( 0 ) != 1 ) {
        return 1;
    }
    if( s.at( 1 ) != 1 ) {
        return 2;
    }
    if( s.at( 2 ) != 3 ) {
        return 3;
    }
    if( s.at( 3 ) != 4 ) {
        return 4;
    }
    if( s.at( 4 ) != 5 ) {
        return 5;
    }
    return 0;
}
