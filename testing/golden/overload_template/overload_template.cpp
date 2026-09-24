#include "overload_template.hpp"
int identity(int value)
{
    return value + 1000;
}
int main()
{
    if( identity( 5 ) != 1005 ) {
        return 1;
    }
    if( identity<float>( 3.0f ) <= 2.9f || identity<float>( 3.0f ) >= 3.1f ) {
        return 2;
    }
    if( scale<int>( 7 ) != 7 ) {
        return 3;
    }
    return 0;
}
