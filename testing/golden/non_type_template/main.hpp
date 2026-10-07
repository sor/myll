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
#include "array_checks.hpp"
template <int N>
inline constexpr int twice = N * 2;
template <int N>
inline int add(int x);
template <int N>
inline int add(int x)
{
    return x + N;
}
