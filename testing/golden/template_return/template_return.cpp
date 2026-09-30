#include "template_return.hpp"
int take(int v)
{
    int ret{};
    ret = v;
    return ret;
}
int main()
{
    int m = max<int>( 3, 4 );
    if( m != 4 ) {
        return 1;
    }
    int e = max<int>( 5, 6 );
    if( e != 6 ) {
        return 2;
    }
    int m2 = max<int>( 1, 2 );
    if( m2 != 2 ) {
        return 3;
    }
    int value = 10;
    int* p = unref<int>( &value );
    if( *p != 10 ) {
        return 4;
    }
    if( take( max<int>( 9, 10 ) ) != 10 ) {
        return 5;
    }
    return 0;
}
