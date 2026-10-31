#include "ctad.hpp"
Box<float> make()
{
    return Box<float>( 1.5f );
}
int main()
{
    Box<int> a( 5 );
    if( a.Get() != 5 ) {
        return 1;
    }
    Box<int> b( 6, 7 );
    if( b.Get() != 6 ) {
        return 2;
    }
    if( b.Get2() != 7 ) {
        return 3;
    }
    Box<int> c( 8, 9, 10 );
    if( c.count != 1 ) {
        return 4;
    }
    auto d = Box<int>( 8 );
    if( d.Get() != 8 ) {
        return 5;
    }
    Box<int> e = Box<int>( 9 );
    if( e.Get() != 9 ) {
        return 6;
    }
    Wrap<int> w( 1.5f, 3 );
    if( w.Value() != 3 ) {
        return 7;
    }
    Pair<int, float> p( 1, 2.5f );
    if( p.First() != 1 ) {
        return 8;
    }
    if( p.Second() < 2.4f ) {
        return 9;
    }
    Box<float> f( 1.5f );
    if( f.Get() < 1.4f ) {
        return 10;
    }
    std::unique_ptr<Box<int>> g = std::make_unique<Box<int>>(11);
    if( g->Get() != 11 ) {
        return 11;
    }
    int x = 5;
    Ptr<int> h( &x );
    if( h._isPtr != 1 ) {
        return 12;
    }
    return 0;
}
