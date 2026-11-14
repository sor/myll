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
#include "myll_dyn_array.hpp"
#include "myll_option.hpp"
#include "myll_result.hpp"
#include "myll_rng.hpp"
#include "myll_string.hpp"
#include "myll_hash.hpp"
#include "myll_map.hpp"
#include "myll_set.hpp"
class Vec2;
class Vec2
{
public:
    int x{};
    int y{};
    Vec2();
    Vec2(int ax, int ay);
    bool operator==(Vec2 other) const;
};
int runOption();
Myll::Result<int, int> divide(int a, int b);
int runResult();
int runRng();
int runDynArray();
int runString();
int runMap();
std::uint64_t hash(Vec2 v);
int runSet();
