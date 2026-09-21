#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <initializer_list>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
#line 3 "../../cases/source_map/one.myll"
struct One;
#line 3 "../../cases/source_map/two.myll"
class Two;
#line 3 "../../cases/source_map/one.myll"
struct One
{
    int value = 10;
#line 4
#line 5
    int half() const;
};
#line 3 "../../cases/source_map/two.myll"
class Two
{
    int value = 20;
#line 4
#line 5
    int twice() const;
};
#line 8 "../../cases/source_map/one.myll"
int oneValue();
#line 8 "../../cases/source_map/two.myll"
int twoValue();
