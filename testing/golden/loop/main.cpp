#include "main.hpp"
int main()
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
    return 0;
}
