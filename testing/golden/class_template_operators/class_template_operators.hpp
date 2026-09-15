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
class Ptr;
class Widget;
template <typename T>
class Ptr
{
    T _value{};
public:
    Ptr() = default;
    inline T& operator*()
    {
        return _value;
    }
    inline T* operator->()
    {
        return &_value;
    }
};
class Widget
{
    int _value = 42;
public:
    Widget() = default;
    int value() const;
};
int main();
