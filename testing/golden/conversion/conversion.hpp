#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
std::uint32_t takes_u32(std::uint32_t x);
std::int64_t takes_i64(std::int64_t x);
void test_widening();
void test_mixed_signed();
void test_arguments();
int main();
