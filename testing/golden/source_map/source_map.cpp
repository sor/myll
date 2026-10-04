#include "source_map.hpp"
#line 8 "../../cases/source_map/one.myll"
NSOne::One::One()
{
#line 8
    doubled = value * 2;
}
#line 9
NSOne::One::One(int v)
{
#line 9
    value = v;
}
#line 10
NSOne::One::~One()
{
}
#line 12
int NSOne::One::half() const
{
#line 12
    return value / 2;
}
#line 9 "../../cases/source_map/two.myll"
NSTwo::Two::Two()
{
#line 9
    tripled = value * 3;
}
#line 10
NSTwo::Two::Two(int v)
{
#line 10
    value = v;
}
#line 11
NSTwo::Two::~Two()
{
}
#line 13
int NSTwo::Two::twice() const
{
#line 13
    return value * 2;
}
#line 20 "../../cases/source_map/one.myll"
int oneValue()
{
#line 21
    return 1;
}
#line 26 "../../cases/source_map/two.myll"
int twoValue()
{
#line 27
    return 2;
}
