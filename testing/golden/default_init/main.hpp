#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class Inner;
class Outer;
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
int main();
