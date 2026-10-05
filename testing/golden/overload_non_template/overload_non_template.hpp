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
namespace overload_const_ptr
{
    int pick(int* p);
    int pick(const int* p);
    int run();
}
namespace overload_const_ref
{
    bool isFive(const int& r);
    int run();
}
namespace overload_exact
{
    int pick(int value);
    int pick(std::uint64_t value);
    int pick(bool value);
    int run();
}
namespace overload_inheritance_conversion
{
    class Base;
    class Derived;
    class Base
    {
    };
    class Derived : public overload_inheritance_conversion::Base
    {
        using base = overload_inheritance_conversion::Base;
    };
    int handle(overload_inheritance_conversion::Base* b);
    int run();
}
namespace overload_inheritance_ptr
{
    class Base;
    class Derived;
    class Base
    {
    };
    class Derived : public overload_inheritance_ptr::Base
    {
        using base = overload_inheritance_ptr::Base;
    };
    int handle(overload_inheritance_ptr::Base* b);
    int handle(overload_inheritance_ptr::Derived* d);
    int run();
}
namespace overload_inheritance_ref
{
    class Base;
    class Derived;
    class Base
    {
    };
    class Derived : public overload_inheritance_ref::Base
    {
        using base = overload_inheritance_ref::Base;
    };
    int handle(overload_inheritance_ref::Base& b);
    int handle(overload_inheritance_ref::Derived& d);
    int run();
}
namespace overload_main
{
    class Gadget;
    class Gadget
    {
        int _id{};
    public:
        explicit Gadget(int value);
        explicit Gadget(float f);
        int id();
        int tag();
        int tag(int bump);
    };
    int dispatch(int x);
    int dispatch(float x);
    int run();
}
namespace overload_pointer
{
    int take(int value);
    int take(int* value);
    int run();
}
namespace overload_simple
{
    int rank(int value);
    int rank(std::uint32_t value);
    int rank(float value);
    int rank(bool value);
    int rank(char value);
    int run();
}
namespace overload_smart_inheritance
{
    class Base;
    class Derived;
    class Base
    {
    };
    class Derived : public overload_smart_inheritance::Base
    {
        using base = overload_smart_inheritance::Base;
    };
    int handle(std::unique_ptr<overload_smart_inheritance::Base> b);
    int handle(std::unique_ptr<overload_smart_inheritance::Derived> d);
    int run();
}
namespace overload_smart_ptr
{
    int take(int* value);
    int take(std::unique_ptr<int> value);
    int take(std::shared_ptr<int> value);
    int run();
}
namespace overload_template_ranking
{
    template <typename T>
    inline int generic(T value);
    int generic(int value);
    int generic(int* value);
    int run();
    template <typename T>
    inline int generic(T value)
    {
        return 1;
    }
}
namespace overload_template
{
    template <typename T>
    inline T identity(T value);
    int identity(int value);
    template <typename T>
    inline T scale(T x);
    int run();
    template <typename T>
    inline T identity(T value)
    {
        return value;
    }
    template <typename T>
    inline T scale(T x)
    {
        return x;
    }
}
namespace ref_binding
{
    int take(int& x);
    int run();
}
namespace ref_category
{
    extern int mutableResult;
    extern int constResult;
    extern int rvalueResult;
    void put(int& x);
    void put(const int& x);
    void assign(const int& value);
    void assign(int&& value);
    int run();
}
