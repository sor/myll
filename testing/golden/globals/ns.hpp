#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
namespace MyNs
{
    extern int normalNs;
    inline int inlineNs = 20;
    const int constNs = 30;
    constexpr const int ctNs = 40;
}
int ns_check();
