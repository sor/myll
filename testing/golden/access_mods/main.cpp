#include "main.hpp"
int main()
{
    Box b{};
    b.update( 2, 3, 4 );
    if( b.getX() != 2 ) {
        return 1;
    }
    if( b.getY() != 3 ) {
        return 2;
    }
    if( b.getZ() != 4 ) {
        return 3;
    }
    if( b.volume() != 24 ) {
        return 4;
    }
    b.setSecret( 42 );
    if( b.getSecret() != 42 ) {
        return 5;
    }
    b.setChildVisible( 7 );
    if( b.getChildVisible() != 7 ) {
        return 6;
    }
    if( b.getSum() != (2 + 3) + (3 + 4) ) {
        return 7;
    }
    return 0;
}
