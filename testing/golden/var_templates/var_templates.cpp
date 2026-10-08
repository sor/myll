#include "var_templates.hpp"
int main()
{
    int a = answer<int>;
    if( a != 42 ) {
        return 1;
    }
    float b = answer<float>;
    if( b < 41.9f || b > 42.1f ) {
        return 2;
    }
    const int* p = ptrValue<int>;
    if( p != nullptr ) {
        return 3;
    }
    Pair<int, int> pair = pairDup<int>;
    if( pair.first != 0 || pair.second != 0 ) {
        return 4;
    }
    if( pi<int> != 3 ) {
        return 5;
    }
    if( pi<float> < 3.4f || pi<float> > 3.6f ) {
        return 6;
    }
    if( pi<std::int64_t> < 3.0f || pi<std::int64_t> > 3.2f ) {
        return 7;
    }
    if( q<int, std::int64_t> != 7 ) {
        return 8;
    }
    if( q<std::int64_t, int> != 70 ) {
        return 9;
    }
    if( q<int, float> != 71 ) {
        return 10;
    }
    if( z<int, std::int64_t> != 17 ) {
        return 11;
    }
    if( z<std::int64_t, int> != 170 ) {
        return 12;
    }
    if( z<int, float> != 171 ) {
        return 13;
    }
    return 0;
}
