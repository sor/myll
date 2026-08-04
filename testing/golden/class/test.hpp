#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class A;
class A
{
public:
    class C;
    class B;
    class C
    {
    public:
        void do_b(B* b);
    };
    class B
    {
    };
};
void do_a(A* a);
