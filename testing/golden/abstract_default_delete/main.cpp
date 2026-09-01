#include "main.hpp"
int Derived::compute()
{
    return 42;
}
Resource::Resource(int v)
{
    _value = v;
}
int Resource::value()
{
    return _value;
}
int dispatch(int x)
{
    return x + 1;
}
int main()
{
    Derived d{};
    if( d.compute() != 42 ) {
        return 1;
    }
    Resource a = Resource( 10 );
    Resource b = Resource( 20 );
    b = a;
    if( b.value() != 10 ) {
        return 2;
    }
    Config cfg{};
    if( !cfg.flag || cfg.count != 7 ) {
        return 3;
    }
    if( dispatch( 5 ) != 6 ) {
        return 4;
    }
    return 0;
}
