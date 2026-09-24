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
class AbstractBase;
class ConcreteDerived;
class Resource;
struct Config;
class Box;
class Alpha;
class Beta;
class Gamma;
class PubDef;
class Priv;
class Prot;
class Virt;
class PubVirt;
class Multi;
class Widget;
class A;
class Base;
class Derived;
class AbstractBase
{
public:
    virtual int compute() const = 0;
};
class ConcreteDerived : public AbstractBase
{
    using base = AbstractBase;
public:
    int compute() const override;
};
class Resource
{
    int _value{};
public:
    explicit Resource(int v);
    Resource& operator=(const Resource& other) = default;
    int value() const;
};
struct Config
{
    bool flag = true;
    int count = 7;
};
class Box
{
    std::int32_t _x = 0;
    std::int32_t _y = 0;
    std::int32_t _z = 0;
    std::int32_t _tag{};
public:
    const std::int32_t maxSize = 100;
private:
    std::int32_t _secret{};
protected:
    std::int32_t _childVisible{};
private:
    void updatePriv(std::int32_t a, std::int32_t b, std::int32_t c);
    std::int32_t helperA() const;
    std::int32_t helperB() const;
protected:
    std::int32_t sum() const;
public:
    std::int32_t getX() const;
    void update(std::int32_t a, std::int32_t b, std::int32_t c);
    std::int32_t volume() const;
    std::int32_t getY() const;
    std::int32_t getZ() const;
    void setSecret(std::int32_t v);
    std::int32_t getSecret() const;
    void setChildVisible(std::int32_t v);
    std::int32_t getChildVisible() const;
    std::int32_t getSum();
};
class Alpha
{
    int a = 1;
};
class Beta
{
    int b = 2;
};
class Gamma
{
    int c = 3;
};
class PubDef : public Alpha
{
    using base = Alpha;
};
class Priv : private Alpha
{
    using base = Alpha;
};
class Prot : protected Alpha
{
    using base = Alpha;
};
class Virt : virtual public Alpha
{
    using base = Alpha;
};
class PubVirt : virtual public Alpha
{
    using base = Alpha;
};
class Multi : public Alpha, private Beta, virtual public Gamma
{
    using base = Alpha;
};
class Widget
{
public:
    int x{};
    int y{};
    Widget();
    Widget(int a, int b);
};
class A
{
public:
    class C;
    class B;
    class C
    {
    public:
        void do_b(A::B* b) const;
    };
    class B
    {
    };
};
class Base
{
    int _baseField = 7;
public:
    int baseOnly();
    virtual int compute(int x);
};
class Derived : public Base
{
    using base = Base;
public:
    int compute(int x) override;
};
int use(AbstractBase* b);
int dispatch(int x);
int dispatch(float x) = delete;
int check_abstract_default_delete();
int check_access_mods();
int check_bases();
int check_class_ctor();
void do_a(A* a);
int check_class();
int useBasePointer(Base* b);
int useBaseReference(Base& b);
int check_inheritance();
