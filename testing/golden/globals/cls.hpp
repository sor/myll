#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class Cls;
class Cls
{
public:
    inline static int inlineField = 7;
    static int normalField;
    inline static constexpr int ctField = 123;
};
extern int extGlobal;
int cls_check();
