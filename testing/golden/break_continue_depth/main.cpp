#include "main.hpp"
int main()
{
    int result = 0;
    int i = 0;
    {
        bool myll_tmp_0 = false;
        while( i < 1 ) {
            i = i + 1;
            int j = 0;
            while( j < 10 ) {
                j = j + 1;
                myll_tmp_0 = true;
                break;
            }
            if( myll_tmp_0 ) {
                break;
            }
            result = result + 100;
        }
    }
    int a = 0;
    int count = 0;
    {
        bool myll_tmp_5 = false;
        while( a < 3 ) {
            a = a + 1;
            int b = 0;
            while( b < 10 ) {
                b = b + 1;
                myll_tmp_5 = true;
                break;
            }
            if( myll_tmp_5 ) {
                continue;
            }
            count = count + 1;
        }
    }
    int x = 0;
    while( x < 1 ) {
        x = x + 1;
        int y = 0;
        while( y < 10 ) {
            y = y + 1;
            result = result + 5;
            break;
        }
    }
    if( result != 5 ) {
        return 1;
    }
    if( a != 3 ) {
        return 2;
    }
    if( count != 0 ) {
        return 3;
    }
    return 0;
}
