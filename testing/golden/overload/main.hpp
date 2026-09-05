#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <initializer_list>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class Gadget;
class Gadget
{
    int _id{};
public:
    explicit Gadget(int value);
    explicit Gadget(float f);
    int id();
    int tag();
    int tag(int bump);
};
int dispatch(int x);
int dispatch(float x);
int main();
