#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
int implicitReturn();
int withFinalReturn();
int withEarlyReturn(int x);
int unusedRet();
int declaredRet();
int paramRet(int ret);
int main();
