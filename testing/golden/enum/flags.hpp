#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
enum class TestFlags;
enum class TestFlagsNumbered;
enum class TestFlagsNumberedPartially;
enum class TestFlags
{
    A = 1,
    B = 2,
    C = 4,
};
enum class TestFlagsNumbered
{
    A = 1,
    B = 2,
    C = 4,
};
enum class TestFlagsNumberedPartially
{
    A = 4,
    B = 8,
    C = 16,
};
inline int flags_test();
inline int flags_test()
{
    if( static_cast<int>( (TestFlags::A) ) != 1 || static_cast<int>( (TestFlags::B) ) != 2 || static_cast<int>( (TestFlags::C) ) != 4 ) {
        return 1;
    }
    if( static_cast<int>( (TestFlagsNumbered::A) ) != 1 || static_cast<int>( (TestFlagsNumbered::B) ) != 2 || static_cast<int>( (TestFlagsNumbered::C) ) != 4 ) {
        return 1;
    }
    if( static_cast<int>( (TestFlagsNumberedPartially::B) ) != 8 || static_cast<int>( (TestFlagsNumberedPartially::C) ) != 16 ) {
        return 1;
    }
    return 0;
}
