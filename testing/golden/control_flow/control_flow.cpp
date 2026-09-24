#include "control_flow.hpp"
int check_break_continue_depth()
{
    int result = 0;
    int i = 0;
    {
        bool myll_tmp_0 = false;
        while( i < 1 ) {
            myll_tmp_0 = false;
            i = i + 1;
            int j = 0;
            while( j < 10 ) {
                myll_tmp_0 = false;
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
            myll_tmp_5 = false;
            a = a + 1;
            int b = 0;
            while( b < 10 ) {
                myll_tmp_5 = false;
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
int check_break_continue_switch()
{
    int failures = 0;
    int x = 1;
    int r1 = 0;
    {
        bool myll_tmp_12 = false;
        switch(x) {
            case 1:
                {
                    int i = 0;
                    while( i < 1 ) {
                        myll_tmp_12 = false;
                        i = i + 1;
                        myll_tmp_12 = true;
                        break;
                    }
                    if( myll_tmp_12 ) {
                        break;
                    }
                    r1 = 99;
                }
                if( myll_tmp_12 ) {
                    break;
                }
                break;
            case 2:
                r1 = 1;
                if( myll_tmp_12 ) {
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
        bool myll_tmp_16 = false;
        while( i2 < 1 ) {
            myll_tmp_16 = false;
            i2 = i2 + 1;
            switch(i2) {
                case 1:
                    myll_tmp_16 = true;
                    break;
            }
            if( myll_tmp_16 ) {
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
        bool myll_tmp_20 = false;
        switch(x3) {
            case 1:
                {
                    switch(x3) {
                        case 1:
                            myll_tmp_20 = true;
                            break;
                    }
                    if( myll_tmp_20 ) {
                        break;
                    }
                    r3 = 99;
                }
                if( myll_tmp_20 ) {
                    break;
                }
                break;
            case 2:
                r3 = 1;
                if( myll_tmp_20 ) {
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
        bool myll_tmp_25 = false;
        while( a < 2 ) {
            myll_tmp_25 = false;
            a = a + 1;
            int b = 0;
            {
                bool myll_tmp_26 = false;
                switch(a) {
                    case 1:
                        {
                            while( b < 5 ) {
                                myll_tmp_25 = false;
                                myll_tmp_26 = false;
                                b = b + 1;
                                myll_tmp_26 = true;
                                myll_tmp_25 = true;
                                break;
                            }
                            if( myll_tmp_25 ) {
                                break;
                            }
                            if( myll_tmp_26 ) {
                                break;
                            }
                            count = count + 1;
                        }
                        if( myll_tmp_25 ) {
                            break;
                        }
                        if( myll_tmp_26 ) {
                            break;
                        }
                        break;
                    default:
                        {
                        }
                        if( myll_tmp_25 ) {
                            break;
                        }
                        if( myll_tmp_26 ) {
                            break;
                        }
                        break;
                }
            }
            if( myll_tmp_25 ) {
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
int check_loop()
{
    int i = 0;
    int sum = 0;
    int count = 0;
    for( ; i < 3; ++i ) {
        ;
    }
    while( ++count < 3 ) {
        ;
    }
    i = 0;
    for( ; i < 3; ++i ) {
        sum = sum + 1;
    }
    count = 0;
    while( count < 3 ) {
        ++count;
    }
    i = 0;
    for( ; i < 3; ++i ) {
        sum = sum + 1;
        count = count + 1;
    }
    count = 0;
    while( count < 3 ) {
        ++count;
        sum = sum + 1;
    }
    count = 0;
    do {
        ++count;
    } while( count < 3 );
    count = 0;
    do {
        ++count;
        sum = sum + 1;
    } while( count < 3 );
    i = 0;
    int contSum = 0;
    while( i < 5 ) {
        ++i;
        if( i == 2 ) {
            continue;
        }
        contSum = contSum + i;
    }
    return 0;
}
int main()
{
    int r = check_loop();
    if( r != 0 ) {
        return r;
    }
    r = check_break_continue_depth();
    if( r != 0 ) {
        return r;
    }
    r = check_break_continue_switch();
    if( r != 0 ) {
        return r;
    }
    r = check_try_catch();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
int check_try_catch()
{
    try
    {
        throw 42;
    }
    catch( int x )
    {
        if( x != 42 ) {
            return 1;
        }
    }
    try
    {
        throw 7;
    }
    catch( ... )
    {
    }
    try
    {
        throw 123;
    }
    catch( int y )
    {
        if( y != 123 ) {
            return 2;
        }
    }
    try
    {
        throw std::exception();
    }
    catch( const std::exception& e )
    {
        if( e.what() == nullptr ) {
            return 3;
        }
    }
    catch( ... )
    {
        return 4;
    }
    return 0;
}
