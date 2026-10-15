#include "main.hpp"
int main()
{
    Box<int> b( 1 );
    if( b.Get() != 20 ) {
        return 1;
    }
    Box<float> c( 1.5f );
    if( c.Get() < 1.4f ) {
        return 2;
    }
    if( b.Read() != 10 ) {
        return 3;
    }
    if( b.Find<float>( 2.5f ) < 2.4f ) {
        return 4;
    }
    if( Box<int>::count != 7 ) {
        return 5;
    }
    Pair<float> p( 1.5f );
    if( p.Get() < 1.9f ) {
        return 6;
    }
    Pair<int> q( 2 );
    if( q.Get() != 2 ) {
        return 7;
    }
    Box<float> d( 1.0f, 2.0f );
    if( d._v2 != 2.0f ) {
        return 8;
    }
    Box<int>::Inner<int> inner{};
    if( inner.W() != 15 ) {
        return 9;
    }
    if( helper() != 90 ) {
        return 10;
    }
    return 0;
}
