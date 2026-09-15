#include "class_template_operators.hpp"
int Widget::value() const
{
    return _value;
}
int main()
{
    Ptr<Widget> p{};
    if( p->value() != 42 ) {
        return 1;
    }
    if( (*p).value() != 42 ) {
        return 2;
    }
    Widget w = *p;
    if( w.value() != 42 ) {
        return 3;
    }
    return 0;
}
