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
class Base;
template <typename T>
class Derived;
template <typename T>
class Outer;
template <typename T>
class Box
{
public:
    T _v{};
    explicit Box(T v)
    {
        _v = v;
    }
    inline T Get() const
    {
        return _v;
    }
    inline T GetTwice() const
    {
        return _v + _v;
    }
    inline Box<T> Self() const
    {
        return *this;
    }
};
template <typename T>
class Base
{
public:
    inline T Zero() const
    {
        return static_cast<T>( 0 );
    }
};
template <typename T>
class Derived : public Base<T>
{
    using base = Base<T>;
};
template <typename T>
class Outer
{
public:
    template <typename V>
    class Inner;
    template <typename V>
    class Inner
    {
    public:
        inline T Combine(T a, V b) const
        {
            return a;
        }
    };
};
