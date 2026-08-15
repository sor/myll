#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
#include <iostream>
#include <cstdlib>
#include <thread>
using milliseconds = std::chrono::milliseconds;
namespace JanSordid
{
    class GameOfLife;
    class GameOfLife
    {
    public:
        using Map2D = char[16][40];
    private:
        const int sizeX = 40;
        const int sizeY = 16;
        std::uint8_t currentIndex = 0;
        Map2D doubleBufferedMap[2];
    public:
        GameOfLife();
        void iterate();
        template <typename T>
        inline void output(T& stream)
        {
            const Map2D& srcMap = doubleBufferedMap[currentIndex];
            for( int y = 0; y < sizeY+0; ++y ) {
                for( int x = 0; x < sizeX+0; ++x ) {
                    stream << srcMap[y][x];
                }
                stream << '\n';
            }
        }
        bool hasConverged() const;
    };
}
int main();
void clear();
