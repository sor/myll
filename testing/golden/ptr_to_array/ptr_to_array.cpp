#include "ptr_to_array.hpp"
int process(int* data, std::uint64_t len)
{
    if( len < 3 ) {
        return 99;
    }
    if( data[0] != 1 ) {
        return 1;
    }
    data = data + 1;
    if( data[0] != 2 ) {
        return 2;
    }
    ++data;
    if( data[0] != 3 ) {
        return 3;
    }
    data = data - 1;
    if( data[0] != 2 ) {
        return 4;
    }
    return 0;
}
int main()
{
    int arr[4]{};
    arr[0] = 1;
    arr[1] = 2;
    arr[2] = 3;
    arr[3] = 4;
    if( process( arr, 4 ) != 0 ) {
        return 5;
    }
    int* p = arr;
    if( p[2] != 3 ) {
        return 6;
    }
    int* raw = &arr[1];
    int* view = static_cast<int*>( raw );
    if( view[0] != 2 ) {
        return 7;
    }
    return 0;
}
