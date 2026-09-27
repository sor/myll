#include "main.hpp"
int main()
{
    if( Pick::pick( 1 ) != 1 ) {
        return 1;
    }
    if( Pick::pick( 2.0f ) != 2 ) {
        return 2;
    }
    return 0;
}
