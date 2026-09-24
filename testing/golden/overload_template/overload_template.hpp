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
inline T identity(T value);
int identity(int value);
template <typename T>
inline T scale(T x);
template <typename T>
inline T identity(T value)
{
    return value;
}
template <typename T>
inline T scale(T x)
{
    return x;
}
