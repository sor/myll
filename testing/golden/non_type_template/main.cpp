#include "main.hpp"
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
    return 0;
}
