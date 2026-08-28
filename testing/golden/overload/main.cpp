#include "main.hpp"
Gadget::Gadget(int value)
{
    _id = value;
}
Gadget::Gadget(float f)
{
    _id = 100;
}
int Gadget::id()
{
    return _id;
}
int Gadget::tag()
{
    return _id * 10;
}
int Gadget::tag(int bump)
{
    return tag() + bump;
}
int dispatch(int x)
{
    return 10;
}
int dispatch(float x)
{
    return 20;
}
int main()
{
    Gadget gi = Gadget( 1 );
    Gadget gf = Gadget( 1.0f );
    if( gi.id() != 1 ) {
        return 1;
    }
    if( gf.id() != 100 ) {
        return 2;
    }
    if( gi.tag( 7 ) != 17 ) {
        return 3;
    }
    if( gf.tag() != 1000 ) {
        return 4;
    }
    if( dispatch( 3 ) != 10 ) {
        return 5;
    }
    if( dispatch( 3.0f ) != 20 ) {
        return 6;
    }
    Gadget* gp = new Gadget( 2 );
    if( gp->tag() != 20 ) {
        return 7;
    }
    if( gp->tag( 3 ) != 23 ) {
        return 8;
    }
    return 0;
}
