#include "cls.hpp"
int Cls::normalField = 11;
int extGlobal = 7;
int cls_check()
{
    static_assert( Cls::ctField == 123, "ctField must be 123" );
    if( Cls::inlineField != 7 ) {
        return 1;
    }
    if( Cls::normalField != 11 ) {
        return 2;
    }
    if( Cls::ctField != 123 ) {
        return 3;
    }
    return 0;
}
