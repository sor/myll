#include "conversion.hpp"
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
int main()
{
    return 0;
}
