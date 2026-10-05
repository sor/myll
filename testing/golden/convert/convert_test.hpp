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
class A;
class B;
class C;
class D;
class E;
class F;
class A
{
public:
    int i{};
    A(int other);
    operator bool() const;
};
class B
{
public:
    int i{};
    B(int other);
};
class C
{
public:
    int i{};
    C(bool b);
};
class D
{
public:
    int i{};
    D(int other);
    operator bool() const;
};
class E
{
public:
    int i{};
    E(int other);
    operator bool() const;
};
class F
{
public:
    int i{};
    explicit F(int other);
    explicit operator bool() const;
};
int run_convert_decl();
bool takesString(const std::string& s);
int run_convert_string();
