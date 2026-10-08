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
template <typename T>
inline T half(T a);
template <typename T, typename U>
inline T combine(T a, U b);
template <typename T>
inline T half(T a)
{
    return a;
}
template <typename T, typename U>
inline T combine(T a, U b)
{
    return a;
}
