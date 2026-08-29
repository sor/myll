#include "main.hpp"
int takesInt(int)
{
    return 42;
}
int main()
{
    int unused = takesInt( 7 );
    return 0;
}
