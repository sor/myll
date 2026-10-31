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
class Wrap;
template <typename A, typename B>
class Pair;
template <typename T>
class Ptr;
template <typename T>
class Box
{
public:
    inline static int count = 0;
    T _v{};
    T _v2{};
    explicit Box(T v)
    {
        _v = v;
    }
    Box(T a, T b)
    {
        _v = a;
        _v2 = b;
    }
    template <typename... U>
    Box(T first, U... rest)
    {
        Box<T>::count = 1;
    }
    inline T Get() const
    {
        return _v;
    }
    inline T Get2() const
    {
        return _v2;
    }
};
template <typename T>
class Wrap
{
public:
    T _v{};
    int _u{};
    template <typename U>
    Wrap(U u, T t)
    {
        _v = t;
        _u = u;
    }
    inline T Value() const
    {
        return _v;
    }
};
template <typename A, typename B>
class Pair
{
public:
    A _a{};
    B _b{};
    Pair(A a, B b)
    {
        _a = a;
        _b = b;
    }
    inline A First() const
    {
        return _a;
    }
    inline B Second() const
    {
        return _b;
    }
};
template <typename T>
class Ptr
{
public:
    T _v{};
    int _isPtr{};
    explicit Ptr(T v)
    {
        _v = v;
        _isPtr = 0;
    }
    explicit Ptr(T* p)
    {
        _v = 0;
        _isPtr = 1;
    }
};
Box<float> make();
