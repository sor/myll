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
#include <limits>
class ByteCount;
class ByteCount
{
public:
    std::uint64_t _v{};
    ByteCount();
    explicit ByteCount(std::uint64_t v);
    std::uint64_t value() const;
};
namespace std
{
    template<>
    inline constexpr ByteCount std::numeric_limits<ByteCount>::max() noexcept
    {
        return ByteCount( 18446744073709551615 );
    }
}
