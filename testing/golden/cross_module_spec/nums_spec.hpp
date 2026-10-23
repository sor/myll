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
template<> inline constexpr int pi<int> = 3;
template <typename T> inline constexpr T q<T, int> = static_cast<T>( 70 );
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
template <typename T>
class Box<T*>
{
public:
    int _value{};
    explicit Box(int v)
    {
        _value = v;
    }
    inline int read() const
    {
        return _value * 2;
    }
};
template<> inline std::int32_t rt<std::int32_t> = 5;
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
