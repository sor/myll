#include "main.hpp"
void test_arithmetic_non_numeric()
{
    int a = true + 1;
}
void test_bitwise_non_integer()
{
    int b = 1 & 1.0f;
}
void test_complement_non_integer()
{
    int c = ~1.0f;
}
void test_incomparable_comparison()
{
    if( "hello" == 1 ) {
    }
}
int main()
{
    return 0;
}
