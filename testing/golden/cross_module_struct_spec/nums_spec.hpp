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
class Box<int>
{
public:
    int _value{};
    int _extra{};
    Box(int v, int e)
    {
        _value = v;
        _extra = e;
    }
    inline int read() const
    {
        return _value + _extra;
    }
};
template <typename T>
class Pair<T, int>
{
public:
    T _a{};
    int _sum{};
    Pair(T a, int s)
    {
        _a = a;
        _sum = s;
    }
};
