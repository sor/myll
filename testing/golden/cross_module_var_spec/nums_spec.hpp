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
#include "nums.hpp"
template<> inline constexpr int pi<int> = 3;
template <typename T> inline constexpr T q<T, int> = static_cast<T>( 70 );
template<> inline std::int32_t rt<std::int32_t> = 5;
