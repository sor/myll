#include "main_span.hpp"
int main(int myll_argc, char* myll_argv[])
{
    std::span<char*> args( myll_argv, myll_argc );
    if( args.size() < 1 ) {
        return 1;
    }
    return 0;
}
