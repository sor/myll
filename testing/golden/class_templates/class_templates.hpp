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
    using IntBox = ref_category::Box<int>;
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
    class Derived : public ref_category::Base<T>
    {
        using base = ref_category::Base<T>;
    };
    int run();
}
namespace spec
{
    template <typename T>
    class Box;
    template <typename T, typename U>
    class Pair;
    template <typename T>
    union Choice;
    class Num;
    template <typename T>
    class SpecBox;
    template <typename T>
    class Rebind;
    template <typename T>
    class Store;
    template <typename T>
    class OpBox;
    template <typename T>
    class Holder;
    template <typename T>
    class Wrap;
    template <typename T>
    class Box
    {
    public:
        T _value{};
        Box() = default;
        inline T read() const
        {
            return _value;
        }
    };
    template<>
    class Box<int>
    {
    public:
        int _value{};
        int _extra{};
        Box(int v, int e)
        {
            _value = v;
            _extra = e;
        }
        inline int read() const
        {
            return _value + _extra;
        }
    };
    template <typename T, typename U>
    class Pair
    {
    public:
        T _a{};
        U _b{};
        Pair() = default;
        inline T sum() const
        {
            return _a;
        }
    };
    template <typename T>
    class Pair<T, int>
    {
    public:
        T _a{};
        int _b{};
        int _sum{};
        Pair(T a, int b)
        {
            _a = a;
            _b = b;
            _sum = 0;
        }
        inline spec::Pair<T, int> add()
        {
            _sum = _sum + _b;
            return *this;
        }
    };
    template <typename T>
    union Choice
    {
        T _val;
        int _idx;
    };
    template<>
    union Choice<float>
    {
        float _val;
        int _idx;
        float _alt;
    };
    class Num
    {
    public:
        int _v{};
        explicit Num(int v);
        template <typename T>
        inline T Get(T t) const
        {
            return t;
        }
    };
    template<>
    inline int spec::Num::Get<int>(int t) const
    {
        return t + _v;
    }
    template <typename T>
    class SpecBox
    {
    public:
        T _v{};
        explicit SpecBox(T v)
        {
            _v = v;
        }
    };
    template<>
    class SpecBox<int>
    {
    public:
        int _v{};
        explicit SpecBox(int v)
        {
            _v = v;
        }
        template <typename U>
        SpecBox(U u, int v)
        {
            _v = v + static_cast<int>( u );
        }
    };
    template <typename T>
    class Rebind
    {
    public:
        T _v{};
        explicit Rebind(T v)
        {
            _v = v;
        }
        inline T twice() const
        {
            return _v + _v;
        }
    };
    template<>
    class Rebind<int>
    {
    public:
        int _v{};
        explicit Rebind(int v)
        {
            _v = v;
        }
        inline int twice() const
        {
            return _v + _v;
        }
    };
    template <typename T>
    class Store
    {
    public:
        T _v{};
        explicit Store(T v)
        {
            _v = v;
        }
        inline T Val(T t) const
        {
            return t;
        }
        template <typename U>
        inline U Load(U u) const
        {
            return u;
        }
    };
    template<>
    inline int spec::Store<int>::Val(int t) const
    {
        return t + _v;
    }
    template<>
    template <typename U>
    inline U spec::Store<int>::Load(U u) const
    {
        return u + 1;
    }
    template<>
    template<>
    inline int spec::Store<int>::Load<int>(int u) const
    {
        return u + 3;
    }
    template <typename T>
    class OpBox
    {
    public:
        T _v{};
        explicit OpBox(T v)
        {
            _v = v;
        }
        inline spec::OpBox<T> operator+(spec::OpBox<T> other) const
        {
            return OpBox( other._v + _v );
        }
    };
    template<>
    inline spec::OpBox<int> spec::OpBox<int>::operator+(spec::OpBox<int> other) const
    {
        return OpBox( other._v + _v + 100 );
    }
    template <typename T>
    class Holder
    {
    public:
        template <typename V>
        class Inner;
        template <typename V>
        class Inner
        {
        public:
            V v{};
        };
        template <typename U>
        inline static constexpr T x = static_cast<U>( 0 );
    };
    template<>
    template<>
    inline constexpr int spec::Holder<int>::x<int> = 7;
    template<>
    template<>
    class spec::Holder<int>::Inner<float>
    {
    public:
        float w{};
    };
    template <typename T>
    class Wrap
    {
    public:
        T _v{};
        explicit Wrap(T v)
        {
            _v = v;
        }
        template <typename U>
        Wrap(U u, T t)
        {
            _v = t;
        }
    };
    template<>
    inline spec::Wrap<int>::Wrap(int v)
    {
        _v = v + 100;
    }
    template<>
    template<>
    inline spec::Wrap<float>::Wrap(std::int32_t u, float t)
    {
        _v = t + 1.5f;
    }
    int run();
    int run4();
    int run3();
    int run2();
}
namespace static_member
{
    template <typename T>
    class Box;
    template <typename T>
    class Box
    {
        inline static int count = 0;
    public:
        static inline int instances()
        {
            return static_member::Box<T>::count;
        }
    private:
        T value{};
    public:
        explicit Box(T v)
        {
            static_member::Box<T>::count = static_member::Box<T>::count + 1;
            value = v;
        }
        Box(const static_member::Box<T>& other)
        {
            value = other.value;
        }
        inline T read()
        {
            return value;
        }
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
