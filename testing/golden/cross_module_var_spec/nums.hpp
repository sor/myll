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
template <typename T>
inline constexpr T pi = static_cast<T>( 3.1415f );
template <typename T, typename U>
inline constexpr T q = static_cast<T>( 7 );
template <typename T>
extern T rt;
