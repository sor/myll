#include "main.hpp"
int main()
{
    int i{};
    int localArr[4]{};
    Inner localInner{};
    if( i != 0 ) {
        return 1;
    }
    if( localInner.value != 0 ) {
        return 2;
    }
    if( localArr[0] != 0 ) {
        return 3;
    }
    Outer o{};
    if( o.scalar != 0 ) {
        return 4;
    }
    if( o.arr[0] != 0 ) {
        return 5;
    }
    if( o.inner.value != 0 ) {
        return 6;
    }
    int raw;
    return 0;
}
