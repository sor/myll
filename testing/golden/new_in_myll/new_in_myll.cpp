#include "new_in_myll.hpp"
Counter::Counter()
{
    value = 0;
}
Counter& Counter::add(int x)
{
    value += x;
    return (*this);
}
const Counter& Counter::peek() const
{
    return (*this);
}
int Counter::retrieve()
{
    return value;
}
int check_bint()
{
    std::uint8_t a = static_cast<std::uint8_t>( 0x0F );
    std::uint8_t b = static_cast<std::uint8_t>( 0x33 );
    if( (a | b) != static_cast<std::uint8_t>( 0x3F ) ) {
        return 1;
    }
    if( (a & b) != static_cast<std::uint8_t>( 0x03 ) ) {
        return 2;
    }
    if( (a & static_cast<std::uint8_t>( ~b )) != static_cast<std::uint8_t>( 0x0C ) ) {
        return 3;
    }
    if( (static_cast<std::uint8_t>( ~a ) | b) != static_cast<std::uint8_t>( 0xF3 ) ) {
        return 4;
    }
    if( ((a) ^ (b)) != static_cast<std::uint8_t>( 0x3C ) ) {
        return 5;
    }
    if( ((a) | (b)) != static_cast<std::uint8_t>( 0x3F ) ) {
        return 6;
    }
    if( ((a) & (b)) != static_cast<std::uint8_t>( 0x03 ) ) {
        return 7;
    }
    if( (a << 1) != static_cast<std::uint8_t>( 0x1E ) ) {
        return 8;
    }
    if( (a >> 1) != static_cast<std::uint8_t>( 0x07 ) ) {
        return 9;
    }
    if( (static_cast<std::uint8_t>( ~a )) != static_cast<std::uint8_t>( 0xF0 ) ) {
        return 10;
    }
    unsigned int m = static_cast<unsigned int>( 0xFFFFFFFF );
    unsigned int n = static_cast<unsigned int>( 0 );
    if( (m & n) != static_cast<std::uint32_t>( 0 ) ) {
        return 11;
    }
    if( (m | n) != static_cast<std::uint32_t>( 0xFFFFFFFF ) ) {
        return 12;
    }
    return 0;
}
int check_char_casts()
{
    char c = 'A';
    int n = static_cast<int>( c );
    if( n != 65 ) {
        return 1;
    }
    char d = static_cast<char>( 66 );
    if( d != 'B' ) {
        return 2;
    }
    std::uint8_t u = static_cast<std::uint8_t>( 'Z' );
    if( u != 90 ) {
        return 3;
    }
    return 0;
}
int check_compound_constants()
{
    std::uint64_t gb = 1024 * 1024 * 1024;
    if( gb != 1073741824 ) {
        return 1;
    }
    std::uint64_t shifted = 0xFF << 8;
    if( shifted != 65280 ) {
        return 2;
    }
    std::uint64_t mixed = 0xF0 | 0x0F;
    if( mixed != 255 ) {
        return 3;
    }
    std::uint64_t remainder = 100 % 7;
    if( remainder != 2 ) {
        return 4;
    }
    std::uint64_t max = 18446744073709551615;
    if( max != 18446744073709551615 ) {
        return 5;
    }
    return 0;
}
int check_default_init()
{
    int i{};
    int localArr[4]{};
    Inner localInner{};
    if( i != 0 ) {
        return 1;
    }
    if( localInner.value != 0 ) {
        return 2;
    }
    if( localArr[0] != 0 ) {
        return 3;
    }
    Outer o{};
    if( o.scalar != 0 ) {
        return 4;
    }
    if( o.arr[0] != 0 ) {
        return 5;
    }
    if( o.inner.value != 0 ) {
        return 6;
    }
    int raw;
    return 0;
}
void setPtr(int* x)
{
    if( x != nullptr ) {
        *x = 42;
    }
}
void setRef(int& x)
{
    x = 99;
}
int getValue()
{
    return 42;
}
int check_discard()
{
    int result = 0;
    setPtr( static_cast<int*>( nullptr ) );
    int myll_tmp_1{};
    setRef( myll_tmp_1 );
    static_cast<void>( getValue() );
    int uninitialized;
    uninitialized = 10;
    for( int myll_tmp_0 = 0; myll_tmp_0 < 3+0; ++myll_tmp_0 ) {
        result = result + 1;
    }
    return result == 3 ? 0 : 1;
}
int main()
{
    int r = check_bint();
    if( r != 0 ) {
        return r;
    }
    r = check_default_init();
    if( r != 0 ) {
        return r;
    }
    r = check_discard();
    if( r != 0 ) {
        return r;
    }
    r = check_return_type_inference();
    if( r != 0 ) {
        return r;
    }
    r = check_compound_constants();
    if( r != 0 ) {
        return r;
    }
    r = check_char_casts();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
void noReturn()
{
}
std::int32_t inferInt(int x)
{
    if( x > 0 ) {
        return 42;
    }
    return 0;
}
std::uint32_t inferUnsigned(std::uint8_t a, std::uint32_t b)
{
    if( a > 0 ) {
        return a;
    }
    return b;
}
int check_return_type_inference()
{
    noReturn();
    int a = inferInt( 1 );
    if( a != 42 ) {
        return 1;
    }
    std::uint8_t small = 1;
    std::uint32_t big = 100;
    std::uint32_t b = inferUnsigned( small, big );
    if( b != 1 ) {
        return 2;
    }
    Counter c{};
    c.add( 1 ).add( 2 ).add( 3 );
    c.peek();
    return c.retrieve() - 6;
}
