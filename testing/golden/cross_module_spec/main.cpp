#include "main.hpp"
int main()
{
    if( pi<int> != 3 ) {
        return 1;
    }
    if( pi<float> < 3.0f || pi<float> > 3.3f ) {
        return 2;
    }
    if( q<std::int64_t, int> != 70 ) {
        return 3;
    }
    if( q<int, std::int64_t> != 7 ) {
        return 4;
    }
    if( rt<std::int32_t> != 5 ) {
        return 5;
    }
    Box<int> b( 3, 4 );
    if( b.read() != 7 ) {
        return 6;
    }
    Box<float> p{};
    p._value = 1.5f;
    if( p.read() < 1.4f ) {
        return 7;
    }
    Pair<float, int> q( 9.5f, 6 );
    if( q._sum != 6 ) {
        return 8;
    }
    Box<int*> pb( 21 );
    if( pb.read() != 42 ) {
        return 9;
    }
    if( half( 21 ) != 42 ) {
        return 10;
    }
    if( combine( 2, 3.5f ) != 203 ) {
        return 11;
    }
    if( half<float>( 2.5f ) < 2.4f || half<float>( 2.5f ) > 2.6f ) {
        return 12;
    }
    if( combine<float, float>( 2.5f, 3.5f ) != 2.5f ) {
        return 13;
    }
    if( half<int>( 3 ) != 6 ) {
        return 14;
    }
    Somelib::Box<int> s{};
    s._v = 3;
    s._extra = 4;
    if( s.Get() != 7 ) {
        return 15;
    }
    Somelib::Box<double> sd{};
    sd._v = 1.5f;
    if( sd.Get() < 1.4f ) {
        return 16;
    }
    return 0;
}
