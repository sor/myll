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
#include "myll_dyn_array.hpp"
#include <vector>
struct Point;
struct Counter;
struct Point
{
    int x = 0;
    int y = 0;
    Point();
    Point(int x_, int y_);
};
struct Counter
{
    int value = 0;
    Counter();
    explicit Counter(int v);
};
int run_direct_ctor();
int takes(std::initializer_list<int> values);
int run_initializer_list();
int run_local_multi_decl();
