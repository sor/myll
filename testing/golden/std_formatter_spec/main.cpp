#include "main.hpp"
Vec2::Vec2(int x, int y)
{
    _x = x;
    _y = y;
}
int main()
{
    std::println( "{}", Vec2( 1, 2 ) );
    return 0;
}
