#include "main.hpp"
int main()
{
    A a;
    do_a( &a );
    A::B b;
    A::C c;
    c.do_b( &b );
    return 0;
}
