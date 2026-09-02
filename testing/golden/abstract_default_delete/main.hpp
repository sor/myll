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
class Resource;
struct Config;
class Base
{
public:
    virtual int compute() = 0;
};
class Derived : public Base
{
    using base = Base;
public:
    int compute() override;
};
class Resource
{
    int _value{};
public:
    explicit Resource(int v);
    Resource& operator=(const Resource& other) = default;
    int value();
};
struct Config
{
    bool flag = true;
    int count = 7;
};
int use(Base* b);
int dispatch(int x);
int dispatch(float x) = delete;
int main();
