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
namespace Other
{
    struct OtherPoint;
    struct OtherPoint
    {
        int x = 0;
        int y = 0;
        OtherPoint();
        OtherPoint(int x_, int y_);
    };
}
