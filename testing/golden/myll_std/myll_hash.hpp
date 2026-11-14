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
    constexpr std::uint64_t hash(bool key)
    {
        return key ? 1 : 0;
    }
    constexpr std::uint64_t hash(char key)
    {
        return static_cast<std::uint64_t>( key );
    }
    constexpr std::uint64_t hash(std::uint8_t key)
    {
        return static_cast<std::uint64_t>( key );
    }
    constexpr std::uint64_t hash(std::uint16_t key)
    {
        return static_cast<std::uint64_t>( key );
    }
    constexpr std::uint64_t hash(std::uint32_t key)
    {
        return static_cast<std::uint64_t>( key );
    }
    constexpr std::uint64_t hash(std::uint64_t key)
    {
        return key;
    }
    constexpr std::uint64_t hash(std::int8_t key)
    {
        return static_cast<std::uint64_t>( static_cast<std::uint8_t>( key ) );
    }
    constexpr std::uint64_t hash(std::int16_t key)
    {
        return static_cast<std::uint64_t>( static_cast<std::uint16_t>( key ) );
    }
    constexpr std::uint64_t hash(std::int32_t key)
    {
        return static_cast<std::uint64_t>( static_cast<std::uint32_t>( key ) );
    }
    constexpr std::uint64_t hash(std::int64_t key)
    {
        return static_cast<std::uint64_t>( key );
    }
    constexpr std::uint64_t hash(Myll::StringView view)
    {
        std::uint64_t result = 14695981039346656037;
        for( int i = 0; i < (view.len())+0; ++i ) {
            result = (result) ^ static_cast<std::uint64_t>( view.at( i ) );
            result = result * 1099511628211;
        }
        return result;
    }
    constexpr std::uint64_t hash(Myll::String str)
    {
        return Myll::hash( str.view() );
    }
}
