#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class A;
class B;
class C;
class PubDef;
class Priv;
class Prot;
class Virt;
class PubVirt;
class Multi;
class A
{
    int a = 1;
};
class B
{
    int b = 2;
};
class C
{
    int c = 3;
};
class PubDef : public A
{
    using base = A;
};
class Priv : private A
{
    using base = A;
};
class Prot : protected A
{
    using base = A;
};
class Virt : virtual public A
{
    using base = A;
};
class PubVirt : virtual public A
{
    using base = A;
};
class Multi : public A, private B, virtual public C
{
    using base = A;
};
int main();
