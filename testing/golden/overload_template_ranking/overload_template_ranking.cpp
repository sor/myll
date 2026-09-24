#include "overload_template_ranking.hpp"
int generic(int value)
{
    return 2;
}
int generic(int* value)
{
    return 3;
}
int main()
{
    if( generic( 42 ) != 2 ) {
        return 1;
    }
    int x = 0;
    if( generic( &x ) != 3 ) {
        return 2;
    }
    if( generic<float>( 3.14f ) != 1 ) {
        return 3;
    }
    return 0;
}
