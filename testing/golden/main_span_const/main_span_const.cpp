#include "main_span_const.hpp"
int main(int myll_argc, const char* myll_argv[])
{
    std::span<const char*> args( myll_argv, myll_argc );
    if( args.size() < 1 ) {
        return 1;
    }
    return 0;
}
