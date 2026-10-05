#include "contained_using_ns.hpp"
int LocalNS::localValue()
{
    return 21;
}
Container::Container()
{
    values.push_back( 1 );
    values.push_back( 2 );
    values.push_back( 3 );
}
int Container::sum()
{
    int total = 0;
    for( auto it : values ) {
        total += it;
    }
    return total;
}
int Outer::Inner::ping()
{
    return 7;
}
int check_module()
{
    if( LocalNS::localValue() != 21 ) {
        return 1;
    }
    return 0;
}
int check_class()
{
    Container c{};
    if( c.sum() != 6 ) {
        return 2;
    }
    return 0;
}
int main()
{
    {
        int result = check_module();
        if( result != 0 ) {
            return result;
        }
    }
    {
        int result = check_class();
        if( result != 0 ) {
            return result;
        }
    }
    {
        int result = run_ns_alias_checks();
        if( result != 0 ) {
            return result;
        }
    }
    return 0;
}
int run_ns_alias_checks()
{
    return Outer::Inner::ping() - 7;
}
