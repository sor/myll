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
class Counter;
class Counter
{
    int value = 10;
public:
    int bumpBy(int n);
};
int loopWork();
