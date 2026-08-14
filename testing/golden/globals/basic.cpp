#include "basic.hpp"
static int hiddenGlobal = 42;
static int hiddenGlobal2 = hiddenGlobal;
int normalGlobal = 1;
int outside_check()
{
    static_assert( ctGlobal == 4, "ctGlobal must be 4" );
    if( hiddenGlobal != 42 ) {
        return 1;
    }
    if( hiddenGlobal2 != 42 ) {
        return 111;
    }
    if( normalGlobal != 1 ) {
        return 2;
    }
    if( inlineGlobal != 2 ) {
        return 3;
    }
    if( constGlobal != 3 ) {
        return 4;
    }
    if( ctGlobal != 4 ) {
        return 5;
    }
    if( extGlobal != 7 ) {
        return 6;
    }
    return 0;
}
