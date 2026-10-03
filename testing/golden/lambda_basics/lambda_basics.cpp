#include "lambda_basics.hpp"
int Counter::bumpBy(int n)
{
    auto readValue = [this]() { return value; };
    value += n;
    return readValue();
}
int loopWork()
{
    auto f = []() {
        int sum = 0;
        int i = 0;
        {
            bool myll_tmp_0 = false;
            while( i < 3 ) {
                myll_tmp_0 = true;
                {
                    ++i;
                    if( i == 1 ) {
                        continue;
                    }
                    sum += i;
                }
            }
            if( !myll_tmp_0 ) {
                sum += 100;
            }
        }
        return sum;
    };
    auto g = []() {
        int found = 0;
        int a = 0;
        {
            bool myll_tmp_3 = false;
            for( ; a < 3; ++a ) {
                myll_tmp_3 = false;
                int b = 0;
                for( ; b < 3; ++b ) {
                    myll_tmp_3 = false;
                    if( a * b == 4 ) {
                        found = 1;
                        myll_tmp_3 = true;
                        break;
                    }
                    if( myll_tmp_3 ) {
                        break;
                    }
                }
                if( myll_tmp_3 ) {
                    break;
                }
            }
        }
        return found;
    };
    return f() + g();
}
int main()
{
    auto add = [](int a, int b) { return a + b; };
    if( add( 1, 2 ) != 3 ) {
        return 1;
    }
    int total = 5;
    auto bump = [&]() {
        total += 10;
        return total;
    };
    if( bump() != 15 ) {
        return 2;
    }
    if( total != 15 ) {
        return 3;
    }
    Counter counter{};
    if( counter.bumpBy( 4 ) != 14 ) {
        return 4;
    }
    if( loopWork() != 6 ) {
        return 6;
    }
    return 0;
}
