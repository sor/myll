#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class Box;
class Box
{
    std::int32_t _x = 0;
    std::int32_t _y = 0;
    std::int32_t _z = 0;
    std::int32_t _tag;
public:
    const std::int32_t maxSize = 100;
private:
    std::int32_t _secret;
protected:
    std::int32_t _childVisible;
private:
    void updatePriv(std::int32_t a, std::int32_t b, std::int32_t c);
    std::int32_t helperA();
    std::int32_t helperB();
protected:
    std::int32_t sum();
public:
    std::int32_t getX();
    void update(std::int32_t a, std::int32_t b, std::int32_t c);
    std::int32_t volume();
    std::int32_t getY();
    std::int32_t getZ();
    void setSecret(std::int32_t v);
    std::int32_t getSecret();
    void setChildVisible(std::int32_t v);
    std::int32_t getChildVisible();
    std::int32_t getSum();
};
