#include "main_span.hpp"
int main(int myll_argc, char* myll_argv[])
{
    std::span<char*> args( myll_argv, myll_argc );
    if( run_span_checks() != 0 ) {
        return 2;
    }
    if( args.size() < 1 ) {
        return 1;
    }
    return 0;
}
int spanSum(std::span<const int> values)
{
    int total = 0;
    std::uint64_t i = 0;
    while( i < values.size() ) {
        total = total + values[i];
        ++i;
    }
    return total;
}
int spanFirstOf(const std::span<const int>& values)
{
    return values[0];
}
int run_span_checks()
{
    int data[5] = { 1, 2, 3, 4, 5 };
    std::span<const int> view( data, 5 );
    if( spanSum( view ) != 15 ) {
        return 1;
    }
    if( view.size() != 5 ) {
        return 2;
    }
    if( view[0] != 1 ) {
        return 3;
    }
    if( view[4] != 5 ) {
        return 4;
    }
    if( spanFirstOf( view ) != 1 ) {
        return 5;
    }
    const int* ptr = view.data();
    if( ptr[0] != 1 ) {
        return 6;
    }
    if( ptr[4] != 5 ) {
        return 7;
    }
    return 0;
}
