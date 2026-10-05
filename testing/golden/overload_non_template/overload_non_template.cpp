#include "overload_non_template.hpp"
int overload_const_ptr::pick(int* p)
{
    return 1;
}
int overload_const_ptr::pick(const int* p)
{
    return 2;
}
int overload_const_ptr::run()
{
    int x = 0;
    int* mutP = &x;
    if( overload_const_ptr::pick( mutP ) != 1 ) {
        return 1;
    }
    const int* constP = &x;
    if( overload_const_ptr::pick( constP ) != 2 ) {
        return 2;
    }
    return 0;
}
bool overload_const_ref::isFive(const int& r)
{
    return static_cast<int>( r ) == 5;
}
int overload_const_ref::run()
{
    int x = 5;
    if( !overload_const_ref::isFive( x ) ) {
        return 1;
    }
    const int cx = 5;
    if( !overload_const_ref::isFive( cx ) ) {
        return 2;
    }
    return 0;
}
int overload_exact::pick(int value)
{
    return 1;
}
int overload_exact::pick(std::uint64_t value)
{
    return 2;
}
int overload_exact::pick(bool value)
{
    return 3;
}
int overload_exact::run()
{
    int a = 0;
    std::uint64_t b = 0;
    bool c = false;
    if( overload_exact::pick( a ) != 1 ) {
        return 1;
    }
    if( overload_exact::pick( b ) != 2 ) {
        return 2;
    }
    if( overload_exact::pick( c ) != 3 ) {
        return 3;
    }
    return 0;
}
int overload_inheritance_conversion::handle(overload_inheritance_conversion::Base* b)
{
    return 1;
}
int overload_inheritance_conversion::run()
{
    overload_inheritance_conversion::Derived* d = new overload_inheritance_conversion::Derived();
    if( overload_inheritance_conversion::handle( d ) != 1 ) {
        return 1;
    }
    overload_inheritance_conversion::Derived dObj{};
    if( overload_inheritance_conversion::handle( &dObj ) != 1 ) {
        return 2;
    }
    return 0;
}
int overload_inheritance_ptr::handle(overload_inheritance_ptr::Base* b)
{
    return 1;
}
int overload_inheritance_ptr::handle(overload_inheritance_ptr::Derived* d)
{
    return 2;
}
int overload_inheritance_ptr::run()
{
    overload_inheritance_ptr::Derived* d = new overload_inheritance_ptr::Derived();
    if( overload_inheritance_ptr::handle( d ) != 2 ) {
        return 1;
    }
    overload_inheritance_ptr::Base* b = d;
    if( overload_inheritance_ptr::handle( b ) != 1 ) {
        return 2;
    }
    return 0;
}
int overload_inheritance_ref::handle(overload_inheritance_ref::Base& b)
{
    return 1;
}
int overload_inheritance_ref::handle(overload_inheritance_ref::Derived& d)
{
    return 2;
}
int overload_inheritance_ref::run()
{
    overload_inheritance_ref::Derived d{};
    overload_inheritance_ref::Derived& dr = d;
    if( overload_inheritance_ref::handle( dr ) != 2 ) {
        return 1;
    }
    overload_inheritance_ref::Base& br = d;
    if( overload_inheritance_ref::handle( br ) != 1 ) {
        return 2;
    }
    return 0;
}
overload_main::Gadget::Gadget(int value)
{
    _id = value;
}
overload_main::Gadget::Gadget(float f)
{
    _id = 100;
}
int overload_main::Gadget::id()
{
    return _id;
}
int overload_main::Gadget::tag()
{
    return _id * 10;
}
int overload_main::Gadget::tag(int bump)
{
    return tag() + bump;
}
int overload_main::dispatch(int x)
{
    return 10;
}
int overload_main::dispatch(float x)
{
    return 20;
}
int overload_main::run()
{
    overload_main::Gadget gi = overload_main::Gadget( 1 );
    overload_main::Gadget gf = overload_main::Gadget( 1.0f );
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
    if( overload_main::dispatch( 3 ) != 10 ) {
        return 5;
    }
    if( overload_main::dispatch( 3.0f ) != 20 ) {
        return 6;
    }
    overload_main::Gadget* gp = new overload_main::Gadget( 2 );
    if( gp->tag() != 20 ) {
        return 7;
    }
    if( gp->tag( 3 ) != 23 ) {
        return 8;
    }
    return 0;
}
int overload_pointer::take(int value)
{
    return 1;
}
int overload_pointer::take(int* value)
{
    return 2;
}
int overload_pointer::run()
{
    int x = 0;
    if( overload_pointer::take( x ) != 1 ) {
        return 1;
    }
    if( overload_pointer::take( &x ) != 2 ) {
        return 2;
    }
    return 0;
}
int overload_simple::rank(int value)
{
    return 1;
}
int overload_simple::rank(std::uint32_t value)
{
    return 2;
}
int overload_simple::rank(float value)
{
    return 3;
}
int overload_simple::rank(bool value)
{
    return 4;
}
int overload_simple::rank(char value)
{
    return 5;
}
int overload_simple::run()
{
    int i = 0;
    std::uint32_t u = static_cast<std::uint32_t>( 0 );
    float f = 0.0f;
    bool b = false;
    char c = 'a';
    if( overload_simple::rank( i ) != 1 ) {
        return 1;
    }
    if( overload_simple::rank( u ) != 2 ) {
        return 2;
    }
    if( overload_simple::rank( f ) != 3 ) {
        return 3;
    }
    if( overload_simple::rank( b ) != 4 ) {
        return 4;
    }
    if( overload_simple::rank( c ) != 5 ) {
        return 5;
    }
    return 0;
}
int overload_smart_inheritance::handle(std::unique_ptr<overload_smart_inheritance::Base> b)
{
    return 1;
}
int overload_smart_inheritance::handle(std::unique_ptr<overload_smart_inheritance::Derived> d)
{
    return 2;
}
int overload_smart_inheritance::run()
{
    std::unique_ptr<overload_smart_inheritance::Derived> d = std::make_unique<overload_smart_inheritance::Derived>();
    if( overload_smart_inheritance::handle( std::move( d ) ) != 2 ) {
        return 1;
    }
    std::unique_ptr<overload_smart_inheritance::Base> b = std::make_unique<overload_smart_inheritance::Base>();
    if( overload_smart_inheritance::handle( std::move( b ) ) != 1 ) {
        return 2;
    }
    return 0;
}
int overload_smart_ptr::take(int* value)
{
    return 1;
}
int overload_smart_ptr::take(std::unique_ptr<int> value)
{
    return 2;
}
int overload_smart_ptr::take(std::shared_ptr<int> value)
{
    return 3;
}
int overload_smart_ptr::run()
{
    int x = 0;
    int* raw = &x;
    if( overload_smart_ptr::take( raw ) != 1 ) {
        return 1;
    }
    std::unique_ptr<int> uniq = std::make_unique<int>();
    if( overload_smart_ptr::take( std::move( uniq ) ) != 2 ) {
        return 2;
    }
    std::shared_ptr<int> shared = std::make_shared<int>();
    if( overload_smart_ptr::take( shared ) != 3 ) {
        return 3;
    }
    return 0;
}
int overload_template_ranking::generic(int value)
{
    return 2;
}
int overload_template_ranking::generic(int* value)
{
    return 3;
}
int overload_template_ranking::run()
{
    if( overload_template_ranking::generic( 42 ) != 2 ) {
        return 1;
    }
    int x = 0;
    if( overload_template_ranking::generic( &x ) != 3 ) {
        return 2;
    }
    if( overload_template_ranking::generic<float>( 3.14f ) != 1 ) {
        return 3;
    }
    return 0;
}
int overload_template::identity(int value)
{
    return value + 1000;
}
int overload_template::run()
{
    if( overload_template::identity( 5 ) != 1005 ) {
        return 1;
    }
    if( overload_template::identity<float>( 3.0f ) <= 2.9f || overload_template::identity<float>( 3.0f ) >= 3.1f ) {
        return 2;
    }
    if( overload_template::scale<int>( 7 ) != 7 ) {
        return 3;
    }
    return 0;
}
int ref_binding::take(int& x)
{
    ++x;
    return x;
}
int ref_binding::run()
{
    int a = 5;
    int b = ref_binding::take( a );
    if( a != 6 ) {
        return 1;
    }
    if( b != 6 ) {
        return 2;
    }
    ++a;
    if( a != 7 ) {
        return 3;
    }
    return 0;
}
int ref_category::mutableResult = 0;
int ref_category::constResult = 0;
int ref_category::rvalueResult = 0;
void ref_category::put(int& x)
{
    ref_category::mutableResult = 1;
}
void ref_category::put(const int& x)
{
    ref_category::constResult = 1;
}
void ref_category::assign(const int& value)
{
    ref_category::constResult += 10;
}
void ref_category::assign(int&& value)
{
    ref_category::rvalueResult += 10;
}
int ref_category::run()
{
    int x = 5;
    ref_category::put( x );
    if( ref_category::mutableResult != 1 || ref_category::constResult != 0 ) {
        return 1;
    }
    const int c = 5;
    ref_category::put( c );
    if( ref_category::constResult != 1 ) {
        return 2;
    }
    ref_category::assign( x );
    if( ref_category::constResult != 11 || ref_category::rvalueResult != 0 ) {
        return 3;
    }
    ref_category::assign( 5 );
    if( ref_category::rvalueResult != 10 ) {
        return 4;
    }
    return 0;
}
int main()
{
    if( overload_main::run() != 0 ) {
        return 1;
    }
    if( overload_exact::run() != 0 ) {
        return 2;
    }
    if( overload_simple::run() != 0 ) {
        return 3;
    }
    if( overload_pointer::run() != 0 ) {
        return 4;
    }
    if( overload_const_ptr::run() != 0 ) {
        return 5;
    }
    if( overload_const_ref::run() != 0 ) {
        return 6;
    }
    if( ref_binding::run() != 0 ) {
        return 7;
    }
    if( ref_category::run() != 0 ) {
        return 8;
    }
    if( overload_inheritance_ptr::run() != 0 ) {
        return 9;
    }
    if( overload_inheritance_ref::run() != 0 ) {
        return 10;
    }
    if( overload_inheritance_conversion::run() != 0 ) {
        return 11;
    }
    if( overload_smart_ptr::run() != 0 ) {
        return 12;
    }
    if( overload_smart_inheritance::run() != 0 ) {
        return 13;
    }
    if( overload_template::run() != 0 ) {
        return 14;
    }
    if( overload_template_ranking::run() != 0 ) {
        return 15;
    }
    return 0;
}
