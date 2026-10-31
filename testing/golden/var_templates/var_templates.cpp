#include "var_templates.hpp"
template <typename T>
T rt = static_cast<T>( 0 );
template <typename T, typename U>
T rtq = static_cast<T>( 7 );
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
    if( tv<int, float> != 1 ) {
        return 20;
    }
    if( tv<int, int*> != 2 ) {
        return 21;
    }
    if( tv<float, float> != 0 ) {
        return 22;
    }
    if( rt<int> != 5 ) {
        return 14;
    }
    if( rtv<int> != 21 ) {
        return 15;
    }
    if( rt<std::int64_t> != 0 ) {
        return 16;
    }
    if( rtq<std::int64_t, int> != 70 ) {
        return 17;
    }
    if( rtq<int, std::int64_t> != 7 ) {
        return 18;
    }
    if( sizeOf<int> != 0 ) {
        return 19;
    }
    if( answer2<int> != 41 ) {
        return 22;
    }
    if( sizeOf<int*> != 8 ) {
        return 20;
    }
    if( sizeOf2<float> < 3.9f ) {
        return 21;
    }
    return 0;
}
