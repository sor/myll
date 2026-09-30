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
inline T max(T a, T b);
template <typename T>
inline T* unref(T* p);
int take(int v);
template <typename T>
inline T max(T a, T b)
{
    T ret{};
    ret = a > b ? a : b;
    return ret;
}
template <typename T>
inline T* unref(T* p)
{
    T* ret{};
    ret = p;
    return ret;
}
