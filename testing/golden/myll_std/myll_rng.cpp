#include "myll_rng.hpp"
Myll::Rng::Rng(std::uint64_t seed)
{
    _state = seed;
}
Myll::Rng::Rng(Myll::Rng&& other)
{
    _state = other._state;
    other._state = 1;
}
Myll::Rng& Myll::Rng::operator=(Myll::Rng&& other)
{
    if( &(*this) == &other ) {
        return (*this);
    }
    _state = other._state;
    other._state = 1;
    return (*this);
}
std::uint64_t Myll::Rng::next()
{
    _state ^= _state >> 12;
    _state ^= _state << 25;
    _state ^= _state >> 27;
    return _state;
}
std::uint32_t Myll::Rng::nextU32()
{
    std::uint64_t value = next();
    return static_cast<std::uint32_t>( (value >> 32) );
}
float Myll::Rng::nextF32()
{
    std::uint64_t value = next();
    return static_cast<float>( ((value >> 40) & 0xFFFFFF) ) / 16777216.0f;
}
double Myll::Rng::nextF64()
{
    std::uint64_t value = next();
    return static_cast<double>( (value >> 11) ) / 9007199254740992.0f;
}
std::uint64_t Myll::Rng::range(std::uint64_t min, std::uint64_t max)
{
    if( max < min ) {
        return min;
    }
    std::uint64_t span = max - min + 1;
    std::uint64_t value = next();
    return min + static_cast<std::uint64_t>( (value % span) );
}
std::uint64_t Myll::Rng::state() const
{
    return _state;
}
