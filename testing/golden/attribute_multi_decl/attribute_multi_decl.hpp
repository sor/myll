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
struct Block;
struct Section;
struct Mixed;
struct Block
{
private:
    int a{};
    int b{};
};
struct Section
{
private:
    int a{};
    int b{};
};
struct Mixed
{
private:
    int a{};
    int b{};
    int c{};
public:
    int d{};
};
