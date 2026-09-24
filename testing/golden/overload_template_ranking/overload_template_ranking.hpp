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
inline int generic(T value);
int generic(int value);
int generic(int* value);
template <typename T>
inline int generic(T value)
{
    return 1;
}
