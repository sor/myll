#include "main.hpp"
int main()
{
    int failures = 0;
    int x = 1;
    int r1 = 0;
    {
        bool myll_tmp_0 = false;
        switch(x) {
            case 1:
                {
                    int i = 0;
                    while( i < 1 ) {
                        myll_tmp_0 = false;
                        i = i + 1;
                        myll_tmp_0 = true;
                        break;
                    }
                    if( myll_tmp_0 ) {
                        break;
                    }
                    r1 = 99;
                }
                if( myll_tmp_0 ) {
                    break;
                }
                break;
            case 2:
                r1 = 1;
                if( myll_tmp_0 ) {
                    break;
                }
                break;
        }
    }
    if( r1 != 0 ) {
        failures = failures + 1;
    }
    int i2 = 0;
    int r2 = 0;
    {
        bool myll_tmp_4 = false;
        while( i2 < 1 ) {
            myll_tmp_4 = false;
            i2 = i2 + 1;
            switch(i2) {
                case 1:
                    myll_tmp_4 = true;
                    break;
            }
            if( myll_tmp_4 ) {
                break;
            }
            r2 = 99;
        }
    }
    if( r2 != 0 ) {
        failures = failures + 1;
    }
    int r3 = 0;
    int x3 = 1;
    {
        bool myll_tmp_8 = false;
        switch(x3) {
            case 1:
                {
                    switch(x3) {
                        case 1:
                            myll_tmp_8 = true;
                            break;
                    }
                    if( myll_tmp_8 ) {
                        break;
                    }
                    r3 = 99;
                }
                if( myll_tmp_8 ) {
                    break;
                }
                break;
            case 2:
                r3 = 1;
                if( myll_tmp_8 ) {
                    break;
                }
                break;
        }
    }
    if( r3 != 0 ) {
        failures = failures + 1;
    }
    int a = 0;
    int count = 0;
    int r4 = 0;
    {
        bool myll_tmp_13 = false;
        while( a < 2 ) {
            myll_tmp_13 = false;
            a = a + 1;
            int b = 0;
            {
                bool myll_tmp_14 = false;
                switch(a) {
                    case 1:
                        {
                            while( b < 5 ) {
                                myll_tmp_13 = false;
                                myll_tmp_14 = false;
                                b = b + 1;
                                myll_tmp_14 = true;
                                myll_tmp_13 = true;
                                break;
                            }
                            if( myll_tmp_13 ) {
                                break;
                            }
                            if( myll_tmp_14 ) {
                                break;
                            }
                            count = count + 1;
                        }
                        if( myll_tmp_13 ) {
                            break;
                        }
                        if( myll_tmp_14 ) {
                            break;
                        }
                        break;
                    default:
                        {
                        }
                        if( myll_tmp_13 ) {
                            break;
                        }
                        if( myll_tmp_14 ) {
                            break;
                        }
                }
            }
            if( myll_tmp_13 ) {
                continue;
            }
            r4 = r4 + 1;
        }
    }
    if( a != 2 ) {
        failures = failures + 1;
    }
    if( count != 0 ) {
        failures = failures + 1;
    }
    if( r4 != 1 ) {
        failures = failures + 1;
    }
    return failures;
}
