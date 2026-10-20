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
template <typename T>
inline constexpr T answer = static_cast<T>( 42 );
template <typename T>
inline constexpr T* ptrValue = nullptr;
template <typename T>
inline constexpr T pi = static_cast<T>( 3.1415f );
template<> inline constexpr int pi<int> = 3;
template<> inline constexpr float pi<float> = 3.5f;
template <typename T, typename U>
inline constexpr T q = static_cast<T>( 7 );
template <typename T> inline constexpr T q<T, int> = static_cast<T>( 70 );
template <typename T> inline constexpr T q<T, float> = static_cast<T>( 71 );
template <typename T, typename U>
inline constexpr T z = static_cast<T>( 17 );
template <typename T> inline constexpr T z<T, int> = static_cast<T>( 170 );
template <typename T> inline constexpr T z<T, float> = static_cast<T>( 171 );
template <typename T>
inline constexpr T rtv = static_cast<T>( 0 );
template<> inline constexpr int rtv<int> = static_cast<int>( 21 );
template <typename T>
inline constexpr T sizeOf = static_cast<T>( 0 );
template <typename T> inline constexpr T sizeOf<T*> = static_cast<T>( 8 );
template <typename T>
inline constexpr T sizeOf2 = static_cast<T>( 0 );
template<> inline constexpr float sizeOf2<float> = static_cast<float>( 4 );
template <typename T>
inline constexpr Pair<T, T> pairDup = Pair<T, T>();
template <typename T, typename U>
class Pair
{
public:
    T first{};
    U second{};
};
template <typename T>
extern T rt;
template<> inline int rt<int> = 5;
template <typename T, typename U>
extern T rtq;
template <typename T> inline T rtq<T, int> = static_cast<T>( 70 );
