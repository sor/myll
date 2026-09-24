#include "language_basics.hpp"
int check_byte()
{
    std::byte a = std::byte{0x0F};
    std::byte b = std::byte{0x33};
    if( (a | b) != std::byte{0x3F} ) {
        return 1;
    }
    if( (a & b) != std::byte{0x03} ) {
        return 2;
    }
    if( (a & std::byte{~b}) != std::byte{0x0C} ) {
        return 3;
    }
    if( (std::byte{~a} | b) != std::byte{0xF3} ) {
        return 4;
    }
    if( ((a) ^ (b)) != std::byte{0x3C} ) {
        return 5;
    }
    if( ((a) | (b)) != std::byte{0x3F} ) {
        return 6;
    }
    if( ((a) & (b)) != std::byte{0x03} ) {
        return 7;
    }
    if( (a << 1) != std::byte{0x1E} ) {
        return 8;
    }
    if( (a >> 1) != std::byte{0x07} ) {
        return 9;
    }
    if( (~a) != std::byte{0xF0} ) {
        return 10;
    }
    return 0;
}
std::uint32_t takes_u32(std::uint32_t x)
{
    return x;
}
std::int64_t takes_i64(std::int64_t x)
{
    return x;
}
void test_widening()
{
    std::int32_t a = 1;
    std::int64_t b = a;
    std::uint16_t c = 2;
    std::uint32_t d = c;
    bool flag = true;
    std::int32_t count = flag;
}
void test_mixed_signed()
{
    std::int32_t x = -2;
    std::uint32_t y = 9000;
    std::int64_t result = x * y;
}
void test_arguments()
{
    std::uint8_t v = 7;
    std::uint32_t r = takes_u32( v );
    std::int32_t s = -1;
    std::int64_t t = takes_i64( s );
}
int check_conversion()
{
    test_widening();
    test_mixed_signed();
    test_arguments();
    return 0;
}
int check_ct()
{
    constexpr const int answer = 42;
    constexpr int doubled = answer * 2;
    static_assert( answer == 42, "answer must be 42" );
    static_assert( doubled == 84, "doubled must be 84" );
    if( answer != 42 ) {
        return 1;
    }
    if( doubled != 84 ) {
        return 2;
    }
    return 0;
}
int check_default_sized()
{
    int i = -5;
    std::int32_t si = i;
    int ri = si;
    unsigned int u = 7;
    std::uint32_t su = u;
    unsigned int ru = su;
    float f = 1.5f;
    float sf = f;
    float rf = sf;
    unsigned int m = static_cast<unsigned int>( 0xFFFFFFFF );
    std::uint32_t sm = m;
    unsigned int rm = sm;
    if( si != -5 ) {
        return 1;
    }
    if( ri != -5 ) {
        return 2;
    }
    if( su != 7 ) {
        return 3;
    }
    if( ru != 7 ) {
        return 4;
    }
    if( sf != 1.5f ) {
        return 5;
    }
    if( rf != 1.5f ) {
        return 6;
    }
    if( sm != static_cast<std::uint32_t>( 0xFFFFFFFF ) ) {
        return 7;
    }
    if( rm != static_cast<unsigned int>( 0xFFFFFFFF ) ) {
        return 8;
    }
    return 0;
}
int check_enum()
{
    Test basic = Test::A;
    TestComma basicComma = TestComma::B;
    if( static_cast<int>( (basic) ) != 0 || static_cast<int>( (basicComma) ) != 1 ) {
        return 1;
    }
    if( static_cast<int>( (TestNumbered::A) ) != 1 || static_cast<int>( (TestNumberedComma::A) ) != 1 || static_cast<int>( (TestNumberedPartially::A) ) != 1 ) {
        return 2;
    }
    if( static_cast<int>( (TestNumbered::B) ) != 2 || static_cast<int>( (TestNumbered::C) ) != 3 ) {
        return 3;
    }
    if( static_cast<int>( (TestNumberedComma::B) ) != 2 || static_cast<int>( (TestNumberedComma::C) ) != 3 ) {
        return 4;
    }
    if( static_cast<int>( (TestNumberedPartially::B) ) != 2 || static_cast<int>( (TestNumberedPartially::C) ) != 3 ) {
        return 5;
    }
    if( static_cast<int>( (TestFlags::A) ) != 1 || static_cast<int>( (TestFlags::B) ) != 2 || static_cast<int>( (TestFlags::C) ) != 4 ) {
        return 6;
    }
    if( static_cast<int>( (TestFlagsNumbered::A) ) != 1 || static_cast<int>( (TestFlagsNumbered::B) ) != 2 || static_cast<int>( (TestFlagsNumbered::C) ) != 4 ) {
        return 7;
    }
    if( static_cast<int>( (TestFlagsNumberedPartially::B) ) != 8 || static_cast<int>( (TestFlagsNumberedPartially::C) ) != 16 ) {
        return 8;
    }
    return 0;
}
int takesInt(int)
{
    return 42;
}
int check_func()
{
    int unused = takesInt( 7 );
    return 0;
}
int check_globals()
{
    int r = outside_check();
    if( r != 0 ) {
        return r;
    }
    r = ns_check();
    if( r != 0 ) {
        return r;
    }
    r = cls_check();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
void test01()
{
    int i = 4;
    int j = 7;
    i + j;
    i * j;
    i - j;
    i / j;
    i % j;
    (i) | (j);
    (i) & (j);
}
int check_int()
{
    test01();
    return 0;
}
int main()
{
    int r = check_byte();
    if( r != 0 ) {
        return r;
    }
    r = check_conversion();
    if( r != 0 ) {
        return r;
    }
    r = check_ct();
    if( r != 0 ) {
        return r;
    }
    r = check_default_sized();
    if( r != 0 ) {
        return r;
    }
    r = check_enum();
    if( r != 0 ) {
        return r;
    }
    r = check_func();
    if( r != 0 ) {
        return r;
    }
    r = check_globals();
    if( r != 0 ) {
        return r;
    }
    r = check_int();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
