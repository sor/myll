#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class Base;
class Derived;
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
int useBasePointer(Base* b);
int useBaseReference(Base& b);
int main();
