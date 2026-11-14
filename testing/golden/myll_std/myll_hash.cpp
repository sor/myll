#include "myll_hash.hpp"
std::uint64_t Myll::hash(bool key)
{
    return key ? 1 : 0;
}
std::uint64_t Myll::hash(char key)
{
    return static_cast<std::uint64_t>( key );
}
std::uint64_t Myll::hash(std::uint8_t key)
{
    return static_cast<std::uint64_t>( key );
}
std::uint64_t Myll::hash(std::uint16_t key)
{
    return static_cast<std::uint64_t>( key );
}
std::uint64_t Myll::hash(std::uint32_t key)
{
    return static_cast<std::uint64_t>( key );
}
std::uint64_t Myll::hash(std::uint64_t key)
{
    return key;
}
std::uint64_t Myll::hash(std::int8_t key)
{
    return static_cast<std::uint64_t>( static_cast<std::uint8_t>( key ) );
}
std::uint64_t Myll::hash(std::int16_t key)
{
    return static_cast<std::uint64_t>( static_cast<std::uint16_t>( key ) );
}
std::uint64_t Myll::hash(std::int32_t key)
{
    return static_cast<std::uint64_t>( static_cast<std::uint32_t>( key ) );
}
std::uint64_t Myll::hash(std::int64_t key)
{
    return static_cast<std::uint64_t>( key );
}
std::uint64_t Myll::hash(Myll::StringView view)
{
    std::uint64_t result = 14695981039346656037;
    for( int i = 0; i < (view.len())+0; ++i ) {
        result = (result) ^ static_cast<std::uint64_t>( view.at( i ) );
        result = result * 1099511628211;
    }
    return result;
}
std::uint64_t Myll::hash(Myll::String str)
{
    return Myll::hash( str.view() );
}
