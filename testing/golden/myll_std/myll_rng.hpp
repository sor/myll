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
namespace Myll
{
    class Rng;
    class Rng
    {
        std::uint64_t _state{};
    public:
        explicit Rng(std::uint64_t seed);
        Rng(Myll::Rng&& other);
        Myll::Rng& operator=(Myll::Rng&& other);
        std::uint64_t next();
        std::uint32_t nextU32();
        float nextF32();
        double nextF64();
        std::uint64_t range(std::uint64_t min, std::uint64_t max);
        std::uint64_t state() const;
    };
}
