#include "main.hpp"
int main()
{
    Box<int> b( 3, 4 );
    if( b.read() != 7 ) {
        return 1;
    }
    Box<float> p{};
    p._value = 1.5f;
    if( p.read() < 1.4f ) {
        return 2;
    }
    Pair<float, int> q( 9.5f, 6 );
    if( q._sum != 6 ) {
        return 3;
    }
    return 0;
}
