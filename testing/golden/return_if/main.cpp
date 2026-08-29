#include "main.hpp"
int abs(int x)
{
    if( x < 0 ) {
        return -x;
    }
    return x;
}
int sign(int x)
{
    if( x > 0 ) {
        return 1;
    }
    if( x < 0 ) {
        return -1;
    }
    return 0;
}
void maybeVoid(bool early)
{
    if( early ) {
        return;
    }
}
int main()
{
    if( abs( -5 ) != 5 ) {
        return 1;
    }
    if( abs( 3 ) != 3 ) {
        return 2;
    }
    if( sign( -7 ) != -1 ) {
        return 3;
    }
    if( sign( 0 ) != 0 ) {
        return 4;
    }
    if( sign( 9 ) != 1 ) {
        return 5;
    }
    maybeVoid( true );
    maybeVoid( false );
    return 0;
}
