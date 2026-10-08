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
class Box;
template <typename T, typename U>
class Pair;
template <typename T>
class Box
{
public:
    T _value{};
    Box() = default;
    inline T read() const
    {
        return _value;
    }
};
template <typename T, typename U>
class Pair
{
public:
    T _a{};
    Pair() = default;
};
