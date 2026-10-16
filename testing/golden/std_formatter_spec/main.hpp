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
#include <format>
#include <print>
#include <cstdio>
class Vec2;
class Vec2
{
public:
    int _x{};
    int _y{};
    Vec2(int x, int y);
};
namespace std
{
    template<>
    class formatter<Vec2>
    {
    public:
        constexpr inline const char* parse(std::basic_format_parse_context<char>& ctx) const
        {
            return ctx.begin();
        }
        template <typename Out>
        inline Out format(const Vec2& v, typename std::basic_format_context<Out, char>& ctx) const
        {
            return ctx.out();
        }
    };
}
