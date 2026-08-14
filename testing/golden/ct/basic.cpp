#include "basic.hpp"
int basic_test()
{
    constexpr const int answer = 42;
    constexpr int doubled = answer * 2;
    if( answer != 42 ) {
        return 1;
    }
    if( doubled != 84 ) {
        return 2;
    }
    return 0;
}
