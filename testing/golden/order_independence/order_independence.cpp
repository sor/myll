#include "order_independence.hpp"
int Outer::sum()
{
    return pubBase.x + privBase.x;
}
int main()
{
    Derived d{};
    NSDerived nsd{};
    Diamond dm{};
    Outer o{};
    Boxed bx{};
    int sum = d.x + nsd.x + dm.left + dm.right + o.sum() + bx.value;
    return sum - 28;
}
