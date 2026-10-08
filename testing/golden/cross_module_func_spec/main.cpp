#include "main.hpp"
int main()
{
    if( half( 21 ) != 42 ) {
        return 1;
    }
    if( combine( 2, 3.5f ) != 203 ) {
        return 2;
    }
    if( half<float>( 2.5f ) < 2.4f || half<float>( 2.5f ) > 2.6f ) {
        return 3;
    }
    if( combine<float, float>( 2.5f, 3.5f ) != 2.5f ) {
        return 4;
    }
    if( half<int>( 3 ) != 6 ) {
        return 5;
    }
    return 0;
}
