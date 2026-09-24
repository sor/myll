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
class A;
class A
{
public:
    int i{};
    A(int other);
    operator bool() const;
};
int run_convert_decl();
bool takesString(const std::string& s);
int run_convert_string();
