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
struct AllNewWidget;
struct AllOldWidget;
struct Point;
struct MaybeNothingWidget;
struct MaybeSpaceshipWidget;
struct SpaceshipWidget;
struct AllNewWidget
{
    int value{};
    bool operator==(const AllNewWidget& other) const = default;
    auto operator<=>(const AllNewWidget& other) const = default;
};
struct AllOldWidget
{
    int value{};
    bool operator==(const AllOldWidget& other) const;
    bool operator!=(const AllOldWidget& other) const;
    bool operator<(const AllOldWidget& other) const;
    bool operator>(const AllOldWidget& other) const;
    bool operator<=(const AllOldWidget& other) const;
    bool operator>=(const AllOldWidget& other) const;
};
struct Point
{
    int x{};
    int y{};
};
struct MaybeNothingWidget
{
    int value{};
};
struct MaybeSpaceshipWidget
{
    int value{};
    bool operator==(const MaybeSpaceshipWidget& other) const;
    auto operator<=>(const MaybeSpaceshipWidget& other) const = default;
};
struct SpaceshipWidget
{
    int value{};
    bool operator==(const SpaceshipWidget& other) const = default;
    auto operator<=>(const SpaceshipWidget& other) const = default;
};
bool operator==(const Point& a, const Point& b);
bool operator!=(const Point& a, const Point& b);
