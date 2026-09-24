#include "raw_imports.hpp"
int main()
{
    if( Helper::rawHelperValue() != 42 ) {
        return 1;
    }
    if( sqrt( 4.0f ) != 2.0f ) {
        return 2;
    }
    return 0;
}
