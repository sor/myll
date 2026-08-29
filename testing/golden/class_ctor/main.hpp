#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class Widget;
class Widget
{
public:
    int x;
    int y;
    Widget();
    Widget(int a, int b);
};
int main();
