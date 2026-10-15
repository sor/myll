#include "spec_a.hpp"
int helper()
{
    Box<int> b( 3 );
    return b.Get() + b.Read();
}
