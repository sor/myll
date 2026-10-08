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
#include "nums.hpp"
template<>
inline int half<int>(int a);
template<>
inline int combine<int, float>(int a, float b);
template<>
inline int half<int>(int a)
{
    return a * 2;
}
template<>
inline int combine<int, float>(int a, float b)
{
    return a * 100 + static_cast<int>( b );
}
