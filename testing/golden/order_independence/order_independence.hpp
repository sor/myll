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
class Outer;
class Base;
class Derived;
class Left;
class Right;
class Diamond;
class NSDerived;
template <typename T>
class Box;
class Boxed;
class Outer
{
    class PrivBase;
public:
    class PubBase;
    class PubBase
    {
    public:
        int x = 5;
    };
private:
    class PrivBase : public Outer::PubBase
    {
        using base = Outer::PubBase;
    public:
        int x = 6;
    };
    Outer::PubBase pubBase{};
    Outer::PrivBase privBase{};
public:
    int sum();
};
class Base
{
public:
    int x = 2;
};
class Derived : public Base
{
    using base = Base;
public:
    int x = 1;
};
class Left
{
public:
    int left = 3;
};
class Right
{
public:
    int right = 4;
};
class Diamond : public Left, public Right
{
    using base = Left;
};
namespace NS
{
    class Base;
    class Base
    {
    public:
        int x = 2;
    };
}
class NSDerived : public NS::Base
{
    using base = NS::Base;
};
template <typename T>
class Box
{
public:
    int value = 7;
};
class Boxed : public Box<int>
{
    using base = Box<int>;
};
