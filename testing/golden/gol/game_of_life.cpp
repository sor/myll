#include "game_of_life.hpp"
JanSordid::GameOfLife::GameOfLife()
{
    JanSordid::GameOfLife::Map2D& dstMap = doubleBufferedMap[currentIndex];
    for( int y = 0; y < sizeY+0; ++y ) {
        for( int x = 0; x < sizeX+0; ++x ) {
            if( 0 < y && y < sizeY - 1 && 0 < x && x < sizeX - 1 ) {
                dstMap[y][x] = (rand() % 4 == 0) ? 'o' : ' ';
            } else {
                dstMap[y][x] = 'X';
            }
        }
    }
}
void JanSordid::GameOfLife::iterate()
{
    const std::uint8_t nextIndex = 1 - currentIndex;
    const JanSordid::GameOfLife::Map2D& srcMap = doubleBufferedMap[currentIndex];
    JanSordid::GameOfLife::Map2D& dstMap = doubleBufferedMap[nextIndex];
    for( int y = 0; y < sizeY+0; ++y ) {
        for( int x = 0; x < sizeX+0; ++x ) {
            if( srcMap[y][x] == 'X' ) {
                dstMap[y][x] = 'X';
            } else {
                const bool aliveSelf = srcMap[y][x] == 'o';
                const int aliveNeighborCount = (srcMap[y - 1][x - 1] == 'o') + (srcMap[y - 1][x] == 'o') + (srcMap[y - 1][x + 1] == 'o') + (srcMap[y][x - 1] == 'o') + (srcMap[y][x + 1] == 'o') + (srcMap[y + 1][x - 1] == 'o') + (srcMap[y + 1][x] == 'o') + (srcMap[y + 1][x + 1] == 'o');
                const bool aliveDst = aliveSelf ? (aliveNeighborCount == 2 || aliveNeighborCount == 3) : (aliveNeighborCount == 3);
                dstMap[y][x] = aliveDst ? 'o' : ' ';
            }
        }
    }
    currentIndex = nextIndex;
}
void JanSordid::GameOfLife::output(std::ostream& stream)
{
    const JanSordid::GameOfLife::Map2D& srcMap = doubleBufferedMap[currentIndex];
    for( int y = 0; y < sizeY+0; ++y ) {
        for( int x = 0; x < sizeX+0; ++x ) {
            stream << srcMap[y][x];
        }
        stream << '\n';
    }
}
bool JanSordid::GameOfLife::hasConverged() const
{
    const JanSordid::GameOfLife::Map2D& zeroMap = doubleBufferedMap[0];
    const JanSordid::GameOfLife::Map2D& oneMap = doubleBufferedMap[1];
    for( int y = 0; y < sizeY+0; ++y ) {
        for( int x = 0; x < sizeX+0; ++x ) {
            if( zeroMap[y][x] != oneMap[y][x] ) {
                return false;
            }
        }
    }
    return true;
}
int main()
{
    using namespace JanSordid;
    std::chrono::milliseconds sleepTime = std::getenv( "MYLL_TEST" ) != nullptr ? std::chrono::milliseconds( 0 ) : std::chrono::milliseconds( 166 );
    JanSordid::GameOfLife gol;
    while( true ) {
        gol.output( std::cout );
        std::cout.flush();
        gol.iterate();
        if( gol.hasConverged() ) {
            return 0;
        }
        std::this_thread::sleep_for( sleepTime );
        clear();
    }
    return 42;
}
void clear()
{
    for( int myll_times_2 = 0; myll_times_2 < 30+0; ++myll_times_2 ) {
        std::cout << "\n";
    }
}
