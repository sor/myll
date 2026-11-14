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
#include "ct_array_size_ctlib.hpp"
#include "ct_array_size_lib.hpp"
struct S;
constexpr int SIZE = 4;
struct S
{
    int arr[SIZE]{};
};
