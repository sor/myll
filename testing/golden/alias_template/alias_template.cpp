#include "alias_template.hpp"
int test_vec()
{
    alias_lib::Vec<int> v{};
    v.push_back( 42 );
    if( v.size() != 1 ) {
        return 1;
    }
    if( v[0] != 42 ) {
        return 2;
    }
    return 0;
}
int test_fixed_array()
{
    alias_lib::FixedArray<int, 4> arr{};
    if( arr.size() != 4 ) {
        return 3;
    }
    arr[0] = 7;
    if( arr[0] != 7 ) {
        return 4;
    }
    return 0;
}
int test_pair_aliases()
{
    alias_types::P<int, float> p{};
    p.first = 1;
    p.second = 2.5f;
    if( p.first != 1 ) {
        return 5;
    }
    if( p.second < 2.4f || p.second > 2.6f ) {
        return 6;
    }
    alias_types::IntPair<float> ip{};
    ip.first = 8;
    ip.second = 9.0f;
    if( ip.first != 8 ) {
        return 7;
    }
    if( ip.second < 8.9f || ip.second > 9.1f ) {
        return 8;
    }
    return 0;
}
int test_alias_chain()
{
    alias_types::VecAlias<int> v{};
    v.push_back( 100 );
    if( v.size() != 1 || v[0] != 100 ) {
        return 9;
    }
    return 0;
}
int test_pointer_alias()
{
    int x = 123;
    alias_types::IntPtr p = &x;
    if( *p != 123 ) {
        return 10;
    }
    return 0;
}
int main()
{
    int r = test_vec();
    if( r != 0 ) {
        return r;
    }
    r = test_fixed_array();
    if( r != 0 ) {
        return r;
    }
    r = test_pair_aliases();
    if( r != 0 ) {
        return r;
    }
    r = test_alias_chain();
    if( r != 0 ) {
        return r;
    }
    r = test_pointer_alias();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
