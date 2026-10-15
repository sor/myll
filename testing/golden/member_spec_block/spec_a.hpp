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
inline Box<int>::Box(int v)
{
    _v = v * 10;
}
template<>
inline int Box<int>::Get() const
{
    return _v * 2;
}
template<>
template<>
inline float Box<int>::Find<float>(float a) const
{
    return a;
}
template<>
inline int Box<int>::count = 7;
template<>
template<>
class Box<int>::Inner<int>
{
public:
    int v{};
    Inner()
    {
        v = 5;
    }
    inline int W() const
    {
        return v * 3;
    }
};
int helper();
