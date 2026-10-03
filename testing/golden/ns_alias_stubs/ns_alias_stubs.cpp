#include "ns_alias_stubs.hpp"
int Outer::Inner::ping()
{
    return 7;
}
int main()
{
    return Outer::Inner::ping() - 7;
}
