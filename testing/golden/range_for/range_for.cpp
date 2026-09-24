#include "range_for.hpp"
int main()
{
    if( run_range_for_auto() != 0 ) {
        return 1;
    }
    if( run_range_for_initlist_auto() != 0 ) {
        return 2;
    }
    return 0;
}
int run_range_for_auto()
{
    std::vector<int> v{};
    v.push_back( 1 );
    v.push_back( 2 );
    v.push_back( 3 );
    int sum = 0;
    for( int i : v ) {
        sum += i;
    }
    if( sum != 6 ) {
        return 1;
    }
    return 0;
}
int run_range_for_initlist_auto()
{
    int total = 0;
    for( std::int32_t i : { 1, 2, 3, 4, 5 } ) {
        total += i;
    }
    if( total != 15 ) {
        return 1;
    }
    return 0;
}
