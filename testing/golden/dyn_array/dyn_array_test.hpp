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
#include "dyn_array.hpp"
#include "other_point.hpp"
struct Point;
struct Point
{
    int x = 0;
    int y = 0;
    Point();
    Point(int x_, int y_);
};
namespace Test
{
    int checkBasic();
    int checkGrowth();
    int checkStack();
    int checkStruct();
    int checkIterator();
    int checkShrink();
    int checkAlgorithms();
    int checkImportedRefMember();
    int checkMove();
}
