#include "main.hpp"
Widget::Widget()
{
    x = 0;
    y = 0;
}
Widget::Widget(int a, int b)
{
    x = a;
    y = b;
}
int main()
{
    Widget a;
    if( a.x != 0 || a.y != 0 ) {
        return 1;
    }
    Widget* b = new Widget( 3, 4 );
    if( b->x != 3 || b->y != 4 ) {
        return 2;
    }
    Widget* c = new Widget( 9, 10 );
    if( c->x != 9 || c->y != 10 ) {
        return 3;
    }
    Widget** d = new Widget*( b );
    if( (*d)->x != 3 || (*d)->y != 4 ) {
        return 4;
    }
    std::unique_ptr<Widget> p = std::make_unique<Widget>(5, 6);
    if( p->x != 5 || p->y != 6 ) {
        return 5;
    }
    Widget* wp = new Widget( 7, 8 );
    std::unique_ptr<Widget>* wpp = new std::unique_ptr<Widget>( wp );
    if( (*wpp)->x != 7 || (*wpp)->y != 8 ) {
        return 6;
    }
    Widget* raw = new Widget( 11, 12 );
    std::unique_ptr<Widget*> upRaw = std::make_unique<Widget*>(raw);
    if( (*upRaw)->x != 11 || (*upRaw)->y != 12 ) {
        return 7;
    }
    Widget* wp2 = new Widget( 13, 14 );
    std::unique_ptr<std::unique_ptr<Widget>> upup = std::make_unique<std::unique_ptr<Widget>>(wp2);
    if( (*upup)->x != 13 || (*upup)->y != 14 ) {
        return 8;
    }
    return 0;
}
