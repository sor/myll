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
struct Pos;
struct Pos
{
    int x = 0;
    int y = 0;
};
void modify(Pos arr[4], int idx, int vx, int vy);
int run_array_of_struct();
bool takesArray(const char* args[]);
int run_array_subscript_ptr(int argc, const char* argv[]);
int run_string_literal_view();
char stringLiteralChar(const char* view);
