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
#include "alias_lib.hpp"
namespace alias_types
{
    template <typename T, typename U>
    struct Pair;
    template <typename T, typename U>
    using P = alias_types::Pair<T, U>;
    template <typename U>
    using IntPair = alias_types::Pair<int, U>;
    template <typename T>
    using VecAlias = typename alias_lib::Vec<T>;
    using IntPtr = int*;
    template <typename T, typename U>
    struct Pair
    {
        T first{};
        U second{};
    };
}
