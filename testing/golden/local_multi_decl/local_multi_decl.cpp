#include "local_multi_decl.hpp"
int main()
{
    std::initializer_list<std::int32_t> a = { 1, 2 };
    std::initializer_list<float> b = { 3.5f };
    if( a.size() != 2 ) {
        return 1;
    }
    if( b.size() != 1 ) {
        return 2;
    }
    std::initializer_list<int> ai = { 1, 2 };
    std::initializer_list<int> bi = { 3, 4 };
    if( ai.size() != 2 ) {
        return 3;
    }
    if( bi.size() != 2 ) {
        return 4;
    }
    int i = 3;
    int j = 4;
    const std::initializer_list<std::int32_t> c = { 1 };
    const std::initializer_list<float> d = { 2.5f };
    if( c.size() != 1 ) {
        return 5;
    }
    if( d.size() != 1 ) {
        return 6;
    }
    return static_cast<int>( a.size() ) + static_cast<int>( b.size() ) + static_cast<int>( ai.size() ) + static_cast<int>( bi.size() ) + static_cast<int>( c.size() ) + static_cast<int>( d.size() ) + i + j - 16;
}
