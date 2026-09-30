#include "same_module_decldef.hpp"
int Test::foo()
{
    return 7;
}
int main()
{
    if( Test::foo() != 7 ) {
        return 1;
    }
    return 0;
}
