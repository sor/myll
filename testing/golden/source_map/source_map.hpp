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
#line 3 "../../cases/source_map/two.myll"
#line 3 "../../cases/source_map/one.myll"
namespace NSOne
{
#line 4
    struct One;
#line 4
    struct One
    {
        int value = 10;
#line 5
#line 7
        One();
#line 8
        explicit One(int v);
#line 9
        ~One();
#line 11
        int half() const;
    };
#line 14
    template <typename T>
    inline T tplFunc(T x);
#line 14
    template <typename T>
    inline T tplFunc(T x)
    {
#line 15
        return x;
    }
}
#line 3 "../../cases/source_map/two.myll"
namespace NSTwo
{
#line 4
    class Two;
#line 14
    template <typename T>
    class Wrapper;
#line 4
    class Two
    {
        int value = 20;
#line 5
#line 7
        Two();
#line 8
        explicit Two(int v);
#line 9
        ~Two();
#line 11
        int twice() const;
    };
#line 14
    template <typename T>
    class Wrapper
    {
        T value{};
#line 15
#line 17
        explicit Wrapper(T v)
        {
#line 17
            value = v;
        }
#line 18
        ~Wrapper()
        {
        }
#line 20
        inline T unwrap() const
        {
#line 20
            return value;
        }
    };
}
#line 19 "../../cases/source_map/one.myll"
int oneValue();
#line 24 "../../cases/source_map/two.myll"
int twoValue();
