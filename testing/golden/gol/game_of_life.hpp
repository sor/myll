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
#include <iostream>
#include <thread>
#include <chrono>
namespace JanSordid
{
    class GameOfLife;
    class GameOfLife
    {
    public:
        using Map2D = char[16][40];
    private:
        const std::int32_t sizeX = 40;
        const std::int32_t sizeY = 16;
        std::uint8_t currentIndex = 0;
        JanSordid::GameOfLife::Map2D doubleBufferedMap[2]{};
    public:
        GameOfLife();
        void iterate();
        void output(std::ostream& stream);
        bool hasConverged() const;
    };
}
void clear();
