#include "static_template_member.hpp"
int main()
{
    Box<int> a = Box<int>( 1 );
    Box<int> b = Box<int>( 2 );
    Box<int> copied = Box<int>( b );
    if( Box<int>::instances() != 2 ) {
        return 1;
    }
    if( a.read() != 1 ) {
        return 2;
    }
    if( copied.read() != 2 ) {
        return 3;
    }
    return 0;
}
