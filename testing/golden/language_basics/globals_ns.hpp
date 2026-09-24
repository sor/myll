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
namespace MyNs
{
    constexpr const int ctNs = 40;
    extern int normalNs;
    inline int inlineNs = 20;
    const int constNs = 30;
}
int ns_check();
