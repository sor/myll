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
#include "myll_string.hpp"
namespace Myll
{
    std::uint64_t hash(bool key);
    std::uint64_t hash(char key);
    std::uint64_t hash(std::uint8_t key);
    std::uint64_t hash(std::uint16_t key);
    std::uint64_t hash(std::uint32_t key);
    std::uint64_t hash(std::uint64_t key);
    std::uint64_t hash(std::int8_t key);
    std::uint64_t hash(std::int16_t key);
    std::uint64_t hash(std::int32_t key);
    std::uint64_t hash(std::int64_t key);
    std::uint64_t hash(Myll::StringView view);
    std::uint64_t hash(Myll::String str);
}
