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
public:
    template <typename V>
    class Inner;
    template <typename V>
    class Inner
    {
    public:
        V v{};
        inline V W() const
        {
            return v;
        }
    };
    inline static int count{};
    T _v{};
    T _v2{};
    explicit Box(T v)
    {
        _v = v;
    }
    Box(T v, T w)
    {
        _v = v;
        _v2 = w;
    }
    inline T Get() const
    {
        return _v;
    }
    template <typename U>
    inline U Find(U a) const
    {
        return a;
    }
    inline T Read() const
    {
        return _v;
    }
};
