#include "synthetic_sugar.hpp"
int LocalNs::localValue()
{
    return 7;
}
int SameFileLib::sameFileValue()
{
    return 21;
}
int InnerScopeTest::innerSum()
{
    return Lib::answer() + Lib::also();
}
int check_alias()
{
    MyInt x = 0;
    if( x != 0 ) {
        return 1;
    }
    if( LocalNs::localValue() != 7 ) {
        return 2;
    }
    return 0;
}
int implicitReturn()
{
    int ret{};
    ret = 42;
    return ret;
}
int withFinalReturn()
{
    int ret{};
    ret = 5;
    return ret;
}
int withEarlyReturn(int x)
{
    int ret{};
    if( x > 0 ) {
        return 1;
    }
    ret = 0;
    return ret;
}
int unusedRet()
{
    return 7;
}
int declaredRet()
{
    int ret = 3;
    return ret;
}
int paramRet(int ret)
{
    return ret + 1;
}
int check_auto_return()
{
    int result = implicitReturn() + withFinalReturn() + withEarlyReturn( 5 ) + withEarlyReturn( -1 ) + unusedRet() + declaredRet() + paramRet( 3 );
    return result == 62 ? 0 : 1;
}
int check_break_if()
{
    int count = 0;
    int a = 0;
    for( a = 0; a < 100; ++a ) {
        if( a == 5 ) {
            break;
        }
        count = count + 1;
    }
    if( count != 5 ) {
        return 1;
    }
    int outerHits = 0;
    int b = 0;
    {
        bool myll_tmp_5 = false;
        for( b = 0; b < 3; ++b ) {
            myll_tmp_5 = false;
            int c = 0;
            for( c = 0; c < 3; ++c ) {
                myll_tmp_5 = false;
                if( c == 1 ) {
                    myll_tmp_5 = true;
                    break;
                }
                if( myll_tmp_5 ) {
                    break;
                }
                outerHits = outerHits + 1;
            }
            if( myll_tmp_5 ) {
                break;
            }
        }
    }
    if( outerHits != 1 ) {
        return 2;
    }
    return 0;
}
int check_continue_if()
{
    int sum = 0;
    int a = 0;
    for( a = 0; a < 10; ++a ) {
        if( a % 2 == 0 ) {
            continue;
        }
        sum = sum + a;
    }
    if( sum != 1 + 3 + 5 + 7 + 9 ) {
        return 1;
    }
    int innerHits = 0;
    int b = 0;
    {
        bool myll_tmp_12 = false;
        for( b = 0; b < 3; ++b ) {
            myll_tmp_12 = false;
            int j = 0;
            while( j < 10 ) {
                myll_tmp_12 = false;
                if( j == 1 ) {
                    myll_tmp_12 = true;
                    break;
                }
                if( myll_tmp_12 ) {
                    break;
                }
                innerHits = innerHits + 1;
                ++j;
            }
            if( myll_tmp_12 ) {
                continue;
            }
        }
    }
    if( innerHits != 3 ) {
        return 2;
    }
    return 0;
}
int check_else_on_loop()
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
int main()
{
    int r = check_alias();
    if( r != 0 ) {
        return r;
    }
    r = check_auto_return();
    if( r != 0 ) {
        return r;
    }
    r = check_break_if();
    if( r != 0 ) {
        return r;
    }
    r = check_continue_if();
    if( r != 0 ) {
        return r;
    }
    r = check_else_on_loop();
    if( r != 0 ) {
        return r;
    }
    r = check_return_if();
    if( r != 0 ) {
        return r;
    }
    r = check_throw_if();
    if( r != 0 ) {
        return r;
    }
    r = check_using();
    if( r != 0 ) {
        return r;
    }
    return 0;
}
int abs(int x)
{
    if( x < 0 ) {
        return -x;
    }
    return x;
}
int sign(int x)
{
    if( x > 0 ) {
        return 1;
    }
    if( x < 0 ) {
        return -1;
    }
    return 0;
}
void maybeVoid(bool early)
{
    if( early ) {
        return;
    }
}
int check_return_if()
{
    if( abs( -5 ) != 5 ) {
        return 1;
    }
    if( abs( 3 ) != 3 ) {
        return 2;
    }
    if( sign( -7 ) != -1 ) {
        return 3;
    }
    if( sign( 0 ) != 0 ) {
        return 4;
    }
    if( sign( 9 ) != 1 ) {
        return 5;
    }
    maybeVoid( true );
    maybeVoid( false );
    return 0;
}
int throwIfValue(bool shouldThrow)
{
    if( shouldThrow ) {
        throw 1;
    }
    return 0;
}
int rethrowIfFlag(bool rethrow)
{
    try
    {
        throwIfValue( true );
    }
    catch( int e )
    {
        if( rethrow ) {
            throw;
        }
        return e;
    }
    return -1;
}
int check_throw_if()
{
    int caught = 0;
    try
    {
        throwIfValue( true );
    }
    catch( int e )
    {
        caught = e;
    }
    if( caught != 1 ) {
        return 1;
    }
    try
    {
        throwIfValue( false );
    }
    catch( int e )
    {
        return 2;
    }
    if( rethrowIfFlag( false ) != 1 ) {
        return 3;
    }
    int rethrown = 0;
    try
    {
        rethrowIfFlag( true );
    }
    catch( int e )
    {
        rethrown = e;
    }
    if( rethrown != 1 ) {
        return 4;
    }
    return 0;
}
int check_using()
{
    int imported = Lib::answer() + Lib::also();
    int sameFile = SameFileLib::sameFileValue() * 2;
    int innerScope = InnerScopeTest::innerSum();
    if( imported != 42 ) {
        return 1;
    }
    if( sameFile != 42 ) {
        return 2;
    }
    if( innerScope != 42 ) {
        return 3;
    }
    return 0;
}
