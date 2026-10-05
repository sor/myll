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
#include <span>
int spanSum(std::span<const int> values);
int spanFirstOf(const std::span<const int>& values);
int run_span_checks();
