#include "main.hpp"
int Base::baseOnly()
{
    return _baseField;
}
int Base::compute(int x)
{
    return x + 1;
}
int Derived::compute(int x)
{
    return x * 10;
}
int useBasePointer(Base* b)
{
    return b->compute( 3 );
}
int useBaseReference(Base& b)
{
    return b.compute( 4 );
}
int main()
{
    Derived d{};
    if( d.baseOnly() != 7 ) {
        return 1;
    }
    if( useBasePointer( &d ) != 30 ) {
        return 2;
    }
    if( useBaseReference( d ) != 40 ) {
        return 3;
    }
    return 0;
}
