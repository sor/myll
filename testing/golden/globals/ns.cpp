#include "ns.hpp"
int MyNs::normalNs = 10;
int ns_check()
{
    static_assert( MyNs::ctNs == 40, "ctNs must be 40" );
    if( MyNs::normalNs != 10 ) {
        return 1;
    }
    if( MyNs::inlineNs != 20 ) {
        return 2;
    }
    if( MyNs::constNs != 30 ) {
        return 3;
    }
    if( MyNs::ctNs != 40 ) {
        return 4;
    }
    return 0;
}
