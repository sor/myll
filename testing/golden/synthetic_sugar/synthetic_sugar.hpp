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
#include "using_helper.hpp"
namespace LocalNs {}
namespace SameFileLib {}
namespace O = LocalNs;
using MyInt = int;
namespace LocalNs
{
    int localValue();
}
namespace SameFileLib
{
    int sameFileValue();
}
namespace InnerScopeTest
{
    using namespace Lib;
    int innerSum();
}
int check_alias();
int implicitReturn();
int withFinalReturn();
int withEarlyReturn(int x);
int unusedRet();
int declaredRet();
int paramRet(int ret);
int check_auto_return();
int check_break_if();
int check_continue_if();
int check_else_on_loop();
int abs(int x);
int sign(int x);
void maybeVoid(bool early);
int check_return_if();
int throwIfValue(bool shouldThrow);
int rethrowIfFlag(bool rethrow);
int check_throw_if();
int check_using();
