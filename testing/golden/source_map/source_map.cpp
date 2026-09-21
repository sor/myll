#include "source_map.hpp"
#line 7 "../../cases/source_map/one.myll"
NSOne::One::One()
{
}
#line 8
NSOne::One::One(int v)
{
#line 8
    value = v;
}
#line 9
NSOne::One::~One()
{
}
#line 11
int NSOne::One::half() const
{
#line 11
    return value / 2;
}
#line 7 "../../cases/source_map/two.myll"
NSTwo::Two::Two()
{
}
#line 8
NSTwo::Two::Two(int v)
{
#line 8
    value = v;
}
#line 9
NSTwo::Two::~Two()
{
}
#line 11
int NSTwo::Two::twice() const
{
#line 11
    return value * 2;
}
#line 19 "../../cases/source_map/one.myll"
int oneValue()
{
#line 20
    return 1;
}
#line 24 "../../cases/source_map/two.myll"
int twoValue()
{
#line 25
    return 2;
}
