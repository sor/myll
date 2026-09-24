#include "ct_array_size.hpp"
int main()
{
    S s{};
    s.arr[0] = 1;
    s.arr[SIZE - 1] = 2;
    if( s.arr[0] != 1 ) {
        return 1;
    }
    if( s.arr[SIZE - 1] != 2 ) {
        return 2;
    }
    int local[SIZE]{};
    local[0] = 3;
    if( local[0] != 3 ) {
        return 3;
    }
    return 0;
}
