#include "init_syntax.hpp"
Point::Point()
{
}
Point::Point(int x_, int y_)
{
    x = x_;
    y = y_;
}
Counter::Counter()
{
    value = 0;
}
Counter::Counter(int v)
{
    value = v;
}
int run_direct_ctor()
{
    Counter c1{};
    if( c1.value != 0 ) {
        return 1;
    }
    Counter c2( 42 );
    if( c2.value != 42 ) {
        return 2;
    }
    Counter c3( c2 );
    if( c3.value != 42 ) {
        return 3;
    }
    Counter c4( 10 );
    Counter c5( 20 );
    if( c4.value != 10 ) {
        return 8;
    }
    if( c5.value != 20 ) {
        return 9;
    }
    Point p1( 1, 2 );
    if( p1.x != 1 ) {
        return 4;
    }
    if( p1.y != 2 ) {
        return 5;
    }
    Myll::DynArray<int> arr( 5 );
    if( arr.capacity() != 5 ) {
        return 6;
    }
    arr.pushBack( 10 );
    if( arr.at( 0 ) != 10 ) {
        return 7;
    }
    return 0;
}
int takes(std::initializer_list<int> values)
{
    return static_cast<int>( values.size() );
}
int run_initializer_list()
{
    auto ints = { 1, 2, 3 };
    if( ints.size() != 3 ) {
        return 13;
    }
    std::initializer_list<int> copied = ints;
    if( copied.size() != 3 ) {
        return 1;
    }
    std::initializer_list<int> moreInts = { 4, 5 };
    if( moreInts.size() != 2 ) {
        return 2;
    }
    std::initializer_list<std::int32_t> aliased = { 6, 7, 8 };
    std::initializer_list<int> copiedAlias = aliased;
    if( copiedAlias.size() != 3 ) {
        return 3;
    }
    if( takes( { 10, 20, 30 } ) != 3 ) {
        return 4;
    }
    if( takes( moreInts ) != 2 ) {
        return 5;
    }
    std::vector<int> vec = { 10, 20, 30 };
    Myll::DynArray<int> arr = { 100, 200, 300 };
    if( arr.size() != 3 ) {
        return 7;
    }
    if( arr.at( 0 ) != 100 ) {
        return 8;
    }
    if( arr.at( 2 ) != 300 ) {
        return 9;
    }
    Myll::DynArray<Point> points = { Point( 1, 2 ), Point( 3, 4 ) };
    if( points.size() != 2 ) {
        return 10;
    }
    if( points.at( 0 ).x != 1 ) {
        return 11;
    }
    if( points.at( 1 ).y != 4 ) {
        return 12;
    }
    return 0;
}
int main()
{
    if( run_direct_ctor() != 0 ) {
        return 1;
    }
    if( run_initializer_list() != 0 ) {
        return 2;
    }
    return 0;
}
