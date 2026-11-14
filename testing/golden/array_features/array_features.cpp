#include "array_features.hpp"
void modify(Pos arr[4], int idx, int vx, int vy)
{
    arr[idx].x = vx;
    arr[idx].y = vy;
}
int run_array_of_struct()
{
    Pos p[4]{};
    p[0].x = 7;
    p[1].y = 3;
    if( p[0].x != 7 ) {
        return 1;
    }
    if( p[1].y != 3 ) {
        return 2;
    }
    if( p[1].x != 0 ) {
        return 3;
    }
    modify( p, 2, 10, 20 );
    if( p[2].x != 10 ) {
        return 4;
    }
    if( p[2].y != 20 ) {
        return 5;
    }
    return 0;
}
bool takesArray(const char* args[])
{
    return args[0] != nullptr;
}
int run_array_subscript_ptr(int argc, const char* argv[])
{
    if( argc < 2 ) {
        return 0;
    }
    if( !takesArray( &argv[1] ) ) {
        return 0;
    }
    if( argv[1] == nullptr ) {
        return 0;
    }
    return 0;
}
int run_string_literal_view()
{
    const char* p = "abc";
    if( p[0] != 'a' || p[2] != 'c' ) {
        return 1;
    }
    const char* q = "abc";
    const char* r = q == nullptr ? "xyz" : "abc";
    if( r[1] != 'b' ) {
        return 2;
    }
    auto s = "auto";
    if( s[1] != 'u' ) {
        return 3;
    }
    if( stringLiteralChar( "xyz" ) != 'x' ) {
        return 4;
    }
    return 0;
}
char stringLiteralChar(const char* view)
{
    return view[0];
}
int main(int argc, const char* argv[])
{
    if( run_array_of_struct() != 0 ) {
        return 1;
    }
    if( run_array_subscript_ptr( argc, argv ) != 0 ) {
        return 2;
    }
    if( run_string_literal_view() != 0 ) {
        return 3;
    }
    return 0;
}
