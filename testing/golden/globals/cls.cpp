#include "cls.hpp"
int Cls::normalField = 11;
int cls_check()
{
    if( Cls::inlineField != 7 ) {
        return 1;
    }
    if( Cls::normalField != 11 ) {
        return 2;
    }
    return 0;
}
