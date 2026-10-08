#include "main.hpp"
int main()
{
    if( pi<int> != 3 ) {
        return 1;
    }
    if( pi<float> < 3.0f || pi<float> > 3.3f ) {
        return 2;
    }
    if( q<std::int64_t, int> != 70 ) {
        return 3;
    }
    if( q<int, std::int64_t> != 7 ) {
        return 4;
    }
    if( rt<std::int32_t> != 5 ) {
        return 5;
    }
    return 0;
}
