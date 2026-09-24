#include "class_operators.hpp"
int main()
{
    BoxLib::Box b{};
    if( (*b).value() != 7 ) {
        return 1;
    }
    BoxLib::Inner i = *b;
    if( i.value() != 7 ) {
        return 2;
    }
    if( b->value() != 7 ) {
        return 3;
    }
    if( BoxLib::Widget( 5 ).readVal() != 5 ) {
        return 4;
    }
    BoxLib::Widget w = BoxLib::Widget( 9 );
    if( w.readVal() != 9 ) {
        return 5;
    }
    return 0;
}
