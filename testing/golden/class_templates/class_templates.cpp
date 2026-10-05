#include "class_templates.hpp"
int operators::Widget::value() const
{
    return _value;
}
int operators::run()
{
    operators::Ptr<operators::Widget> p{};
    if( p->value() != 42 ) {
        return 1;
    }
    if( (*p).value() != 42 ) {
        return 2;
    }
    operators::Widget w = *p;
    if( w.value() != 42 ) {
        return 3;
    }
    return 0;
}
int ref_category::run()
{
    ref_category::Box<int> b{};
    int x = 5;
    b.assign( x );
    if( b.retrieve() != 5 ) {
        return 1;
    }
    b.assign( 10 );
    if( b.retrieve() != 10 ) {
        return 2;
    }
    b.store<float>( 1.5f, 7 );
    if( b.retrieve() != 7 ) {
        return 3;
    }
    ref_category::Derived<int> d{};
    d.baseAssign( x );
    d.baseAssign( 20 );
    ref_category::IntBox ab{};
    ab.assign( 30 );
    if( ab.retrieve() != 30 ) {
        return 7;
    }
    ref_category::IntBox2 ab2{};
    ab2.assign( 40 );
    if( ab2.retrieve() != 40 ) {
        return 8;
    }
    ref_category::IntBox* ap = new ref_category::IntBox();
    ap->assign( 50 );
    if( ap->retrieve() != 50 ) {
        return 9;
    }
    return 0;
}
int static_member::run()
{
    static_member::Box<int> a = static_member::Box<int>( 1 );
    static_member::Box<int> b = static_member::Box<int>( 2 );
    static_member::Box<int> copied = static_member::Box<int>( b );
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
int template_call::run()
{
    template_call::Wrapper<int> w{};
    return w.bar();
}
int main()
{
    if( operators::run() != 0 ) {
        return 1;
    }
    if( ref_category::run() != 0 ) {
        return 2;
    }
    if( template_call::run() != 0 ) {
        return 3;
    }
    if( static_member::run() != 0 ) {
        return 4;
    }
    return 0;
}
