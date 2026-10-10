#include "non_type_template.hpp"
int main()
{
    if( add<5>( 10 ) != 15 ) {
        return 1;
    }
    if( twice<21> != 42 ) {
        return 2;
    }
    if( array_checks::checkStdArray() != 0 ) {
        return 3;
    }
    if( twice<8> != 5 ) {
        return 4;
    }
    if( folded<9> != 111 ) {
        return 5;
    }
    if( twice<4> != 8 ) {
        return 6;
    }
    Fixed<int, 3> f{};
    if( f.len() != 30 ) {
        return 7;
    }
    if( f.first() != 1 ) {
        return 8;
    }
    Fixed<float, 9> g{};
    if( g.len() != 9 ) {
        return 9;
    }
    return 0;
}
