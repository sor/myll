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
namespace deduction
{
    template <typename T>
    inline T max(T a, T b);
    template <typename T>
    inline T first(T* ptr);
    int run();
    template <typename T>
    inline T max(T a, T b)
    {
        return a > b ? a : b;
    }
    template <typename T>
    inline T first(T* ptr)
    {
        return *ptr;
    }
}
namespace overload
{
    int dispatch(int value);
    template <typename T>
    inline int dispatch(const T& value);
    template <typename T>
    inline T identity(const T& value);
    int inspect(int value);
    template <typename T>
    inline int inspect(T* ptr);
    int run();
    template <typename T>
    inline int dispatch(const T& value)
    {
        return 1;
    }
    template <typename T>
    inline T identity(const T& value)
    {
        return value;
    }
    template <typename T>
    inline int inspect(T* ptr)
    {
        return 11;
    }
}
namespace templates
{
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
    int run();
    template <typename T>
    inline T max(T a, T b)
    {
        return a > b ? a : b;
    }
}
