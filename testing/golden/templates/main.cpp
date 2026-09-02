#include "main.hpp"
int main()
{
    int m = max<int>( 1, 2 );
    Container<int> c{};
    c.put( 5 );
    int v = c.take();
    Container<float> cf{};
    cf.put( 3.14f );
    float f = cf.take();
    return m == 2 && v == 5 && f > 3.13f && f < 3.15f ? 0 : 1;
}
