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
class PrivateClass;
struct MutNum;
constinit int ctCounter = 5;
class PrivateClass
{
    void secret() const;
};
struct MutNum
{
    int v{};
    explicit MutNum(int value);
    MutNum operator+(const MutNum& other) const;
};
int test_private_call();
int test_const_operator();
int test_constinit_static_assert();
