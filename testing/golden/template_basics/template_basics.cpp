#include "template_basics.hpp"
int deduction::run()
{
    int a = deduction::max<int>( 1, 2 );
    if( a != 2 ) {
        return 1;
    }
    int x = 42;
    int* p = &x;
    if( deduction::first<int>( p ) != 42 ) {
        return 2;
    }
    return 0;
}
int overload::dispatch(int value)
{
    return 0;
}
int overload::inspect(int value)
{
    return 10;
}
int overload::run()
{
    if( overload::dispatch( 7 ) != 0 ) {
        return 1;
    }
    if( overload::dispatch<const char*>( "hello" ) != 1 ) {
        return 2;
    }
    int n = 42;
    if( overload::identity<int>( n ) != 42 ) {
        return 3;
    }
    float f = 2.5f;
    if( overload::identity<float>( f ) != 2.5f ) {
        return 4;
    }
    if( overload::inspect( 5 ) != 10 ) {
        return 5;
    }
    int* p = &n;
    if( overload::inspect<int>( p ) != 11 ) {
        return 6;
    }
    return 0;
}
int templates::run()
{
    int m = templates::max<int>( 1, 2 );
    templates::Container<int> c{};
    c.put( 5 );
    int v = c.take();
    templates::Container<float> cf{};
    cf.put( 3.14f );
    float f = cf.take();
    return m == 2 && v == 5 && f > 3.13f && f < 3.15f ? 0 : 1;
}
int main()
{
    if( templates::run() != 0 ) {
        return 1;
    }
    if( deduction::run() != 0 ) {
        return 2;
    }
    if( overload::run() != 0 ) {
        return 3;
    }
    return 0;
}
