#include "main.hpp"
int main()
{
    std::uint8_t a = 0x0F;
    std::uint8_t b = 0x33;
    if( (a | b) != 0x3F ) {
        return 1;
    }
    if( (a & b) != 0x03 ) {
        return 2;
    }
    if( (a & static_cast<std::uint8_t>( ~b )) != 0x0C ) {
        return 3;
    }
    if( (static_cast<std::uint8_t>( ~a ) | b) != 0xF3 ) {
        return 4;
    }
    if( (a ^ b) != 0x3C ) {
        return 5;
    }
    if( (a | b) != 0x3F ) {
        return 6;
    }
    if( (a & b) != 0x03 ) {
        return 7;
    }
    if( (a << 1) != 0x1E ) {
        return 8;
    }
    if( (a >> 1) != 0x07 ) {
        return 9;
    }
    if( (static_cast<std::uint8_t>( ~a )) != 0xF0 ) {
        return 10;
    }
    unsigned int m = 0xFFFFFFFF;
    unsigned int n = 0;
    if( (m & n) != 0 ) {
        return 11;
    }
    if( (m | n) != 0xFFFFFFFF ) {
        return 12;
    }
    return 0;
}
