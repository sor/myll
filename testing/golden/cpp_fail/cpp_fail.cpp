#include "cpp_fail.hpp"
void A::secret()
{
}
int main()
{
    A a;
    a.secret();
    return 0;
}
