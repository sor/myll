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
template <typename T>
class Box
{
    inline static int count = 0;
public:
    static inline int instances()
    {
        return Box<T>::count;
    }
private:
    T value{};
public:
    explicit Box(T v)
    {
        Box<T>::count = Box<T>::count + 1;
        value = v;
    }
    Box(const Box<T>& other)
    {
        value = other.value;
    }
    inline T read()
    {
        return value;
    }
};
