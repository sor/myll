#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
enum class Test;
enum class TestComma;
enum class Test
{
    A,
    B,
    C,
};
enum class TestComma
{
    A,
    B,
    C,
};
inline int basic_test();
inline int basic_test()
{
    Test basic = Test::A;
    TestComma basicComma = TestComma::B;
    if( static_cast<int>( (basic) ) != 0 || static_cast<int>( (basicComma) ) != 1 ) {
        return 1;
    }
    return 0;
}
