#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
extern int normalGlobal;
inline int inlineGlobal = 2;
const int constGlobal = 3;
constexpr const int ctGlobal = 4;
int outside_check();
