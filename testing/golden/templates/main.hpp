#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <initializer_list>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
template <typename T>
class Container;
template <typename T>
class Container
{
    T value{};
public:
    inline void put(T v)
    {
        value = v;
    }
    inline T take()
    {
        return value;
    }
};
template <typename T>
inline T max(T a, T b);
int main();
template <typename T>
inline T max(T a, T b)
{
    return a > b ? a : b;
}
