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
class BaseOuter;
template <typename T>
class BaseDerived;
template <typename T>
class ComplexOuter;
template <typename T>
class DependentOuter;
template <typename T>
class NestedOuter;
template <typename T>
class ParamOuter;
template <typename T>
class ReturnOuter;
template <typename T>
class VarOuter;
template <typename T>
class BaseOuter
{
public:
    class Base;
    class Base
    {
    public:
        T value{};
    };
};
template <typename T>
class BaseDerived : public BaseOuter<T>::Base
{
    using base = typename BaseOuter<T>::Base;
public:
    T extra{};
};
template <typename T>
class ComplexOuter
{
public:
    template <typename U>
    class Item;
    template <typename U>
    class Item
    {
    public:
        T key{};
        U value{};
    };
};
template <typename T>
class DependentOuter
{
public:
    class Inner;
    class Inner
    {
    public:
        T value{};
    };
};
template <typename T>
class NestedOuter
{
public:
    template <typename U>
    class Inner;
    template <typename U>
    class Inner
    {
    public:
        T a{};
        U b{};
    };
};
template <typename T>
class ParamOuter
{
public:
    class Inner;
    class Inner
    {
    public:
        T value{};
        inline void assign(T v)
        {
            value = v;
        }
    };
};
template <typename T>
class ReturnOuter
{
public:
    class Inner;
    class Inner
    {
    public:
        T value{};
    };
};
template <typename T>
class VarOuter
{
public:
    class Inner;
    class Inner
    {
    public:
        T x{};
    };
};
int checkBase();
template <typename T, typename U>
inline typename ComplexOuter<T>::template Item<U> makeComplex(T k, U v);
template <typename T, typename U>
inline typename ComplexOuter<T>::template Item<U> duplicateComplex(typename ComplexOuter<T>::template Item<U> src);
int checkComplex();
template <typename T>
inline typename DependentOuter<T>::Inner makeDependent(T v);
int checkDependentInner();
template <typename T, typename U>
inline typename NestedOuter<T>::template Inner<U> testNestedTpl(T a, U b);
int checkNestedTpl();
template <typename T>
inline int testParam(typename ParamOuter<T>::Inner i, T v);
int checkParam();
template <typename T>
inline typename ReturnOuter<T>::Inner testReturn(T v);
int checkReturn();
template <typename T>
inline int testVar(T v);
int checkVar();
template <typename T, typename U>
inline typename ComplexOuter<T>::template Item<U> makeComplex(T k, U v)
{
    typename ComplexOuter<T>::template Item<U> item{};
    item.key = k;
    item.value = v;
    return item;
}
template <typename T, typename U>
inline typename ComplexOuter<T>::template Item<U> duplicateComplex(typename ComplexOuter<T>::template Item<U> src)
{
    return src;
}
template <typename T>
inline typename DependentOuter<T>::Inner makeDependent(T v)
{
    typename DependentOuter<T>::Inner result{};
    result.value = v;
    return result;
}
template <typename T, typename U>
inline typename NestedOuter<T>::template Inner<U> testNestedTpl(T a, U b)
{
    typename NestedOuter<T>::template Inner<U> result{};
    result.a = a;
    result.b = b;
    return result;
}
template <typename T>
inline int testParam(typename ParamOuter<T>::Inner i, T v)
{
    i.assign( v );
    return i.value == v ? 0 : 1;
}
template <typename T>
inline typename ReturnOuter<T>::Inner testReturn(T v)
{
    typename ReturnOuter<T>::Inner result{};
    result.value = v;
    return result;
}
template <typename T>
inline int testVar(T v)
{
    typename VarOuter<T>::Inner item{};
    item.x = v;
    return item.x == v ? 0 : 1;
}
