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
template <typename T, typename U>
class Pair;
template <typename T, typename U>
class Pair
{
public:
    T first{};
    U second{};
};
template <typename T>
const T answer = static_cast<T>( 42 );
template <typename T>
const T* ptrValue = nullptr;
template <typename T>
const Pair<T, T> pairDup = Pair<T, T>();
