#include "main.hpp"
int main()
{
    int result = 0;
    int i = 0;
    {
        bool myll_tmp_0 = false;
        for( i = 0; i < 0; i++ ) {
            myll_tmp_0 = true;
            {
                result = 100;
            }
        }
        if( !myll_tmp_0 ) {
            result = result + 1;
        }
    }
    {
        bool myll_tmp_1 = false;
        while( false ) {
            myll_tmp_1 = true;
            {
                result = 100;
            }
        }
        if( !myll_tmp_1 ) {
            result = result + 2;
        }
    }
    int j = 0;
    {
        bool myll_tmp_2 = false;
        for( j = 0; j < 1; j++ ) {
            myll_tmp_2 = true;
            {
                result = result + 10;
            }
        }
        if( !myll_tmp_2 ) {
            result = 1000;
        }
    }
    return result == 13 ? 0 : 1;
}
