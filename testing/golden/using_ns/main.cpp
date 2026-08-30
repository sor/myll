#include "main.hpp"
int main()
{
    int sum = Lib::answer() + Lib::also();
    if( sum != 42 ) {
        return 1;
    }
    return 0;
}
