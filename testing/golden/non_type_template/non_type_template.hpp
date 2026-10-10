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
template <typename T, int N>
class Fixed;
template <int N>
inline constexpr int twice = N * 2;
template<> inline constexpr int twice<8> = 5;
template <typename T, int N>
class Fixed
{
public:
    Fixed()
    {
    }
    inline int len() const
    {
        return N;
    }
    inline T first() const
    {
        return 0;
    }
};
template<>
class Fixed<int, 3>
{
public:
    Fixed()
    {
    }
    inline int len() const
    {
        return 30;
    }
    inline int first() const
    {
        return 1;
    }
};
template <int N>
inline int add(int x);
template <int N>
inline int add(int x)
{
    return x + N;
}
