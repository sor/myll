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
#include "spec_a.hpp"
template <typename T>
class Pair;
template <typename T>
class Pair
{
public:
    T _v{};
    explicit Pair(T v)
    {
        _v = v;
    }
    inline T Get() const
    {
        return _v;
    }
};
template<>
inline float Pair<float>::Get() const
{
    return _v + 0.5f;
}
