#include "main.hpp"
int main()
{
    int r = basic_test();
    if( r != 0 ) {
        return r;
    }
    r = numbered_test();
    if( r != 0 ) {
        return r;
    }
    return flags_test();
}
