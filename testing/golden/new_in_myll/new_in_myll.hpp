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
class Inner;
class Outer;
class Counter;
class Inner
{
public:
    int value{};
};
class Outer
{
public:
    Inner inner{};
    int scalar{};
    int arr[4]{};
};
class Counter
{
    int value{};
public:
    Counter();
    Counter& add(int x);
    const Counter& peek() const;
    int retrieve();
};
int check_bint();
int check_default_init();
void setPtr(int* x);
void setRef(int& x);
int getValue();
int check_discard();
void noReturn();
std::int32_t inferInt(int x);
std::uint32_t inferUnsigned(std::uint8_t a, std::uint32_t b);
int check_return_type_inference();
