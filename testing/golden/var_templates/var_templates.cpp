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
    return 0;
}
