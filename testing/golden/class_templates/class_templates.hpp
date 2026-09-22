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
namespace operators
{
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
    int run();
}
namespace ref_category
{
    template <typename T>
    class Box;
    template <typename T>
    class Base;
    template <typename T>
    class Derived;
    using IntBox = Box<int>;
    using IntBox2 = ref_category::IntBox;
    template <typename T>
    class Box
    {
        T _value{};
    public:
        inline void assign(const T& value)
        {
            _value = value;
        }
        inline void assign(T&& value)
        {
            _value = std::move( value );
        }
        inline T retrieve()
        {
            return _value;
        }
        template <typename U>
        inline void store(U x, T y)
        {
            _value = y;
        }
    };
    template <typename T>
    class Base
    {
    public:
        inline void baseAssign(const T& value)
        {
        }
        inline void baseAssign(T&& value)
        {
        }
    };
    template <typename T>
    class Derived : public Base<T>
    {
        using base = Base<T>;
    };
    int run();
}
namespace template_call
{
    template <typename T>
    class Wrapper;
    template <typename T>
    class Wrapper
    {
    public:
        template <typename U>
        inline U foo()
        {
            U value{};
            return value;
        }
        inline int bar()
        {
            return (*this).template foo<int>();
        }
    };
    int run();
}
int main();
