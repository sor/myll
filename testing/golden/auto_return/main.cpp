#include "main.hpp"
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
int main()
{
    int result = implicitReturn() + withFinalReturn() + withEarlyReturn( 5 ) + withEarlyReturn( -1 ) + unusedRet() + declaredRet() + paramRet( 3 );
    return result == 62 ? 0 : 1;
}
