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
inline constexpr T pi = static_cast<T>( 3.1415f );
template <typename T, typename U>
inline constexpr T q = static_cast<T>( 7 );
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
template <typename T>
extern T rt;
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
