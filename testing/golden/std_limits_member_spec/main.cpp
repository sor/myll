#include "main.hpp"
ByteCount::ByteCount()
{
    _v = 0;
}
ByteCount::ByteCount(std::uint64_t v)
{
    _v = v;
}
std::uint64_t ByteCount::value() const
{
    return _v;
}
int main()
{
    if( std::numeric_limits<ByteCount>::max().value() != 18446744073709551615 ) {
        return 1;
    }
    if( std::numeric_limits<ByteCount>::min().value() != 0 ) {
        return 2;
    }
    if( std::numeric_limits<std::uint64_t>::max() != 18446744073709551615 ) {
        return 3;
    }
    return 0;
}
