#include "main.hpp"
int Lib::answer()
{
    return 42;
}
int main()
{
    using namespace Lib;
    if( Lib::answer() != 42 ) {
        return 1;
    }
    return 0;
}
