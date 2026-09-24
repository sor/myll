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
namespace Algo
{
    template <typename It>
    inline void sortRange(It first, It last);
    template <typename It>
    inline void sortRange(It first, It last)
    {
        if( first == last ) {
            return;
        }
        It i = first;
        while( i != last ) {
            It j = i;
            ++j;
            while( j != last ) {
                if( *j < *i ) {
                    auto tmp = *i;
                    *i = *j;
                    *j = tmp;
                }
                ++j;
            }
            ++i;
        }
    }
}
