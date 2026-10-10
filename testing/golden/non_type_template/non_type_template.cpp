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
    if( folded<9> != 111 ) {
        return 6;
    }
    if( folded<-5> != -5 ) {
        return 7;
    }
    if( twice<4> != 8 ) {
        return 6;
    }
    Fixed<int, 3> f{};
    if( f.len() != 30 ) {
        return 8;
    }
    if( f.first() != 1 ) {
        return 9;
    }
    Fixed<float, 9> g{};
    if( g.len() != 9 ) {
        return 10;
    }
    Fixed<double, 4> h{};
    if( h.len() != 4 ) {
        return 11;
    }
    return 0;
}
