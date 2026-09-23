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
struct Num;
struct Num
{
    int v{};
    explicit Num(int value);
    Num operator+(const Num& other) const;
    Num& operator+=(const Num& other);
    Num& operator++();
};
