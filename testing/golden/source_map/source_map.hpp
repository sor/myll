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
#line 3 "../../cases/source_map/one.myll"
namespace NSOne
{
#line 4
    struct One;
#line 4
    struct One
    {
#line 5
        int value = 10;
        int doubled = 20;
#line 8
        One();
        explicit One(int v);
        ~One();
#line 12
        int half() const;
    };
#line 15
    template <typename T>
    inline T tplFunc(T x);
#line 15
    template <typename T>
    inline T tplFunc(T x)
    {
#line 16
        return x;
    }
}
#line 3 "../../cases/source_map/two.myll"
namespace NSTwo
{
#line 4
    class Two;
#line 16
    template <typename T>
    class Wrapper;
#line 4
    class Two
    {
#line 5
        int value = 20;
#line 7
        int tripled = 60;
#line 9
        Two();
        explicit Two(int v);
        ~Two();
#line 13
        int twice() const;
    };
#line 16
    template <typename T>
    class Wrapper
    {
#line 17
        T value{};
#line 19
        explicit Wrapper(T v)
        {
#line 19
            value = v;
        }
#line 20
        ~Wrapper()
        {
        }
#line 22
        inline T unwrap() const
        {
#line 22
            return value;
        }
    };
}
#line 20 "../../cases/source_map/one.myll"
int oneValue();
#line 26 "../../cases/source_map/two.myll"
int twoValue();
