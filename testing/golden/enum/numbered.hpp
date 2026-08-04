#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
enum class TestNumbered;
enum class TestNumberedComma;
enum class TestNumberedPartially;
enum class TestNumbered
{
    A = 1,
    B = 2,
    C = 3,
};
enum class TestNumberedComma
{
    A = 1,
    B = 2,
    C = 3,
};
enum class TestNumberedPartially
{
    A = 1,
    B,
    C,
};
inline int numbered_test();
inline int numbered_test()
{
    if( static_cast<int>( (TestNumbered::A) ) != 1 || static_cast<int>( (TestNumberedComma::A) ) != 1 || static_cast<int>( (TestNumberedPartially::A) ) != 1 ) {
        return 1;
    }
    if( static_cast<int>( (TestNumbered::B) ) != 2 || static_cast<int>( (TestNumbered::C) ) != 3 ) {
        return 1;
    }
    if( static_cast<int>( (TestNumberedComma::B) ) != 2 || static_cast<int>( (TestNumberedComma::C) ) != 3 ) {
        return 1;
    }
    if( static_cast<int>( (TestNumberedPartially::B) ) != 2 || static_cast<int>( (TestNumberedPartially::C) ) != 3 ) {
        return 1;
    }
    return 0;
}
