#include "main.hpp"
int main()
{
    int* raw = nullptr;
    if( raw == nullptr ) {
    } else {
        return 1;
    }
    if( raw != nullptr ) {
        return 2;
    }
    raw = new int();
    *raw = 7;
    if( raw != nullptr ) {
    } else {
        return 3;
    }
    if( *raw != 7 ) {
        return 4;
    }
    std::unique_ptr<int> uniq = nullptr;
    if( uniq != nullptr ) {
        return 5;
    }
    uniq = std::make_unique<int>();
    *uniq = 8;
    if( uniq == nullptr ) {
        return 6;
    }
    if( *uniq != 8 ) {
        return 7;
    }
    return 0;
}
