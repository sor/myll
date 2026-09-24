#include "oo_basics.hpp"
int ConcreteDerived::compute() const
{
    return 42;
}
Resource::Resource(int v)
{
    _value = v;
}
int Resource::value() const
{
    return _value;
}
void Box::updatePriv(std::int32_t a, std::int32_t b, std::int32_t c)
{
    _x = a;
    _y = b;
    _z = c;
}
std::int32_t Box::helperA() const
{
    return _x + _y;
}
std::int32_t Box::helperB() const
{
    return _y + _z;
}
std::int32_t Box::sum() const
{
    return helperA() + helperB();
}
std::int32_t Box::getX() const
{
    return _x;
}
void Box::update(std::int32_t a, std::int32_t b, std::int32_t c)
{
    updatePriv( a, b, c );
}
std::int32_t Box::volume() const
{
    return _x * _y * _z;
}
std::int32_t Box::getY() const
{
    return _y;
}
std::int32_t Box::getZ() const
{
    return _z;
}
void Box::setSecret(std::int32_t v)
{
    _secret = v;
}
std::int32_t Box::getSecret() const
{
    return _secret;
}
void Box::setChildVisible(std::int32_t v)
{
    _childVisible = v;
}
std::int32_t Box::getChildVisible() const
{
    return _childVisible;
}
std::int32_t Box::getSum()
{
    return sum();
}
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
void A::C::do_b(A::B* b) const
{
}
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
int use(AbstractBase* b)
{
    return b->compute();
}
int dispatch(int x)
{
    return x + 1;
}
int check_abstract_default_delete()
{
    ConcreteDerived d{};
    if( d.compute() != 42 ) {
        return 1;
    }
    if( use( &d ) != 42 ) {
        return 5;
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
int check_access_mods()
{
    Box b{};
    b.update( 2, 3, 4 );
    if( b.getX() != 2 ) {
        return 1;
    }
    if( b.getY() != 3 ) {
        return 2;
    }
    if( b.getZ() != 4 ) {
        return 3;
    }
    if( b.volume() != 24 ) {
        return 4;
    }
    b.setSecret( 42 );
    if( b.getSecret() != 42 ) {
        return 5;
    }
    b.setChildVisible( 7 );
    if( b.getChildVisible() != 7 ) {
        return 6;
    }
    if( b.getSum() != (2 + 3) + (3 + 4) ) {
        return 7;
    }
    return 0;
}
int check_bases()
{
    return 0;
}
int check_class_ctor()
{
    Widget a{};
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
void do_a(A* a)
{
}
int check_class()
{
    A a{};
    do_a( &a );
    A::B b{};
    A::C c{};
    c.do_b( &b );
    return 0;
}
int useBasePointer(Base* b)
{
    return b->compute( 3 );
}
int useBaseReference(Base& b)
{
    return b.compute( 4 );
}
int check_inheritance()
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
int main()
{
    int r = check_access_mods();
    if( r != 0 ) {
        return r;
    }
    r = check_abstract_default_delete();
    if( r != 0 ) {
        return r;
    }
    r = check_bases();
    if( r != 0 ) {
        return r;
    }
    r = check_class();
    if( r != 0 ) {
        return r;
    }
    r = check_class_ctor();
    if( r != 0 ) {
        return r;
    }
    r = check_inheritance();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
