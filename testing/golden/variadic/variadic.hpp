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
class Builder;
class Builder
{
public:
    static int count;
    template <typename... T>
    explicit Builder(T... args)
    {
        Builder::count = 3;
    }
};
template <typename... T>
inline std::string relay(T... args);
template <typename... T>
inline std::string relayEmpty(T... args);
template <typename... T>
inline std::string forwardChain(T... args);
template <typename T, typename... U>
inline void logMixed(T first, U... rest);
template <typename T>
inline T takeOne(T x);
template <typename... T>
inline std::string relay(T... args)
{
    return std::format( "{}|{}", args... );
}
template <typename... T>
inline std::string relayEmpty(T... args)
{
    return std::format( "empty", args... );
}
template <typename... T>
inline std::string forwardChain(T... args)
{
    return relay( std::forward<T>( args )... );
}
template <typename T, typename... U>
inline void logMixed(T first, U... rest)
{
    std::println( "first {} rest {}", first, rest... );
}
template <typename T>
inline T takeOne(T x)
{
    return x;
}
