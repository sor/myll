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
#include <vector>
#include <array>
namespace alias_lib
{
    template <typename T>
    using Vec = typename std::vector<T>;
    template <typename T, int N>
    using FixedArray = typename std::array<T, N>;
}
