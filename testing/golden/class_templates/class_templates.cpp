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
spec::Num::Num(int v)
{
    _v = v;
}
int spec::run()
{
    spec::Box<int> a( 3, 4 );
    if( a.read() != 7 ) {
        return 1;
    }
    spec::Box<float> b{};
    b._value = 1.5f;
    if( b.read() < 1.4f || b.read() > 1.6f ) {
        return 2;
    }
    spec::Pair<float, int> p( 1.5f, 2 );
    p.add();
    if( p._sum != 2 ) {
        return 3;
    }
    spec::Pair<float, float> q{};
    q._a = 1.5f;
    q._b = 2.5f;
    if( q.sum() < 1.4f ) {
        return 4;
    }
    spec::Choice<float> c{};
    c._alt = 9.5f;
    if( c._alt < 9.0f || c._alt > 10.0f ) {
        return 5;
    }
    spec::Choice<int> d{};
    d._idx = 3;
    if( d._idx != 3 ) {
        return 6;
    }
    spec::PtrBox<int*> w( 41 );
    if( w.read() != 42 ) {
        return 7;
    }
    spec::PtrBox<int> v( 7 );
    if( v.read() != 7 ) {
        return 8;
    }
    if( IsPtr<int>::value ) {
        return 9;
    }
    if( !IsPtr<int*>::value ) {
        return 10;
    }
    return 0;
}
int spec::run4()
{
    spec::Holder<int> h{};
    if( Holder<int>::x<int> != 7 ) {
        return 1;
    }
    if( h.x<int> != 7 ) {
        return 2;
    }
    spec::Holder<float> h2{};
    if( h2.x<int> != 0 ) {
        return 3;
    }
    Holder<int>::Inner<float> i{};
    if( i.w < 0.0f || i.w > 0.1f ) {
        return 4;
    }
    Holder<float>::Inner<std::int32_t> j{};
    if( j.v != 0 ) {
        return 5;
    }
    return 0;
}
int spec::run3()
{
    spec::Wrap<int> a( 1 );
    if( a._v != 101 ) {
        return 1;
    }
    spec::Wrap<float> b( 2.5f );
    if( b._v < 2.4f ) {
        return 2;
    }
    spec::Wrap<float> d( 1, 2.5f );
    if( d._v < 3.9f || d._v > 4.1f ) {
        return 3;
    }
    spec::Wrap<std::int64_t> e( 4, 5 );
    if( e._v != 5 ) {
        return 4;
    }
    spec::Wrap<std::int64_t> f = spec::Wrap<std::int64_t>( 6, 7 );
    if( f._v != 7 ) {
        return 5;
    }
    spec::SpecBox<int> sa( 3 );
    if( sa._v != 3 ) {
        return 6;
    }
    spec::SpecBox<int> sb( 1.5f, 4 );
    if( sb._v != 5 ) {
        return 7;
    }
    spec::Rebind<int> r( 21 );
    if( r.twice() != 42 ) {
        return 8;
    }
    return 0;
}
int spec::run2()
{
    spec::Num n( 2 );
    if( n.Get<int>( 40 ) != 42 ) {
        return 1;
    }
    if( n.Get<float>( 1.5f ) < 1.4f ) {
        return 2;
    }
    spec::Store<int> s( 100 );
    if( s.Val( 1 ) != 101 ) {
        return 3;
    }
    if( s.Load( 1 ) != 4 ) {
        return 4;
    }
    if( s.Load<float>( 2.5f ) < 3.4f ) {
        return 5;
    }
    spec::Store<float> t( 1.5f );
    if( t.Val( 2.5f ) < 2.4f ) {
        return 6;
    }
    if( t.Load<int>( 1 ) != 1 ) {
        return 7;
    }
    spec::OpBox<int> a( 1 );
    spec::OpBox<int> b( 2 );
    spec::OpBox<int> r = a + b;
    if( r._v != 103 ) {
        return 8;
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
    if( spec::run() != 0 ) {
        return 5;
    }
    if( spec::run2() != 0 ) {
        return 6;
    }
    return 0;
}
