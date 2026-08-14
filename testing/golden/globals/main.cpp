#include "main.hpp"
int main()
{
    int r = outside_check();
    if( r != 0 ) {
        return r;
    }
    r = ns_check();
    if( r != 0 ) {
        return r;
    }
    r = cls_check();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
