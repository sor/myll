#include "main.hpp"
int main()
{
    std::byte a = std::byte{0x0F};
    std::byte b = std::byte{0x33};
    if( (a | b) != std::byte{0x3F} ) {
        return 1;
    }
    if( (a & b) != std::byte{0x03} ) {
        return 2;
    }
    if( (a & std::byte{~b}) != std::byte{0x0C} ) {
        return 3;
    }
    if( (std::byte{~a} | b) != std::byte{0xF3} ) {
        return 4;
    }
    if( (a ^ b) != std::byte{0x3C} ) {
        return 5;
    }
    if( (a | b) != std::byte{0x3F} ) {
        return 6;
    }
    if( (a & b) != std::byte{0x03} ) {
        return 7;
    }
    if( (a << 1) != std::byte{0x1E} ) {
        return 8;
    }
    if( (a >> 1) != std::byte{0x07} ) {
        return 9;
    }
    if( (~a) != std::byte{0xF0} ) {
        return 10;
    }
    return 0;
}
