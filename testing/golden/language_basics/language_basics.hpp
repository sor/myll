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
#include "globals_extern.hpp"
#include "globals_classes.hpp"
#include "globals_ns.hpp"
enum class Test;
enum class TestComma;
enum class TestNumbered;
enum class TestNumberedComma;
enum class TestNumberedPartially;
enum class TestFlags;
enum class TestFlagsNumbered;
enum class TestFlagsNumberedPartially;
enum class TestFlagsHex;
enum class TestFlagsComposite;
enum class Test
{
    A,
    B,
    C,
};
enum class TestComma
{
    A,
    B,
    C,
};
enum class TestNumbered
{
    A = 1,
    B = 2,
    C = 3,
};
enum class TestNumberedComma
{
    A = 1,
    B = 2,
    C = 3,
};
enum class TestNumberedPartially
{
    A = 1,
    B,
    C,
};
enum class TestFlags
{
    A = 1,
    B = 2,
    C = 4,
};
enum class TestFlagsNumbered
{
    A = 1,
    B = 2,
    C = 4,
};
enum class TestFlagsNumberedPartially
{
    A = 4,
    B = 8,
    C = 16,
};
enum class TestFlagsHex
{
    A = 0x4,
    B = 0x10,
    C = 32,
};
enum class TestFlagsComposite
{
    Red = 1,
    Green = 2,
    Blue = 4,
    Yellow = (TestFlagsComposite::Red) | (TestFlagsComposite::Green),
    Mask = (TestFlagsComposite::Red) | (TestFlagsComposite::Green) | (TestFlagsComposite::Blue),
    Extra = 8,
};
int check_byte();
std::uint32_t takes_u32(std::uint32_t x);
std::int64_t takes_i64(std::int64_t x);
void test_widening();
void test_mixed_signed();
void test_arguments();
int check_conversion();
int check_ct();
int check_default_sized();
int check_enum();
int takesInt(int);
int check_func();
int check_globals();
void test01();
int check_int();
