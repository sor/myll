#include "main.hpp"
int main()
{
    Somelib::Box<int> b{};
    b._v = 3;
    b._extra = 4;
    if( b.Get() != 7 ) {
        return 1;
    }
    Somelib::Box<double> d{};
    d._v = 1.5f;
    if( d.Get() < 1.4f ) {
        return 2;
    }
    return 0;
}
