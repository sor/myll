#include "member_return_type.hpp"
int main()
{
    Box<int> b( 5 );
    auto x = b.Get();
    if( x != 5 ) {
        return 1;
    }
    if( b.Self().Get() != 5 ) {
        return 2;
    }
    auto y = b.GetTwice();
    if( y != 10 ) {
        return 3;
    }
    Box<int>* p = new Box<int>( 7 );
    if( p->Get() != 7 ) {
        return 4;
    }
    Derived<int> d{};
    if( d.Zero() != 0 ) {
        return 5;
    }
    Outer<int>::Inner<float> inner{};
    if( inner.Combine( 4, 1.5f ) != 4 ) {
        return 6;
    }
    return 0;
}
