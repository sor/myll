#include "array_checks.hpp"
int array_checks::checkStdArray()
{
    std::array<int, 4> arr{};
    if( arr.size() != 4 ) {
        return 1;
    }
    arr[0] = 42;
    if( arr[0] != 42 ) {
        return 2;
    }
    return 0;
}
