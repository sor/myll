#include "cpp_fail.hpp"
void A::secret() const
{
}
int main()
{
    A a{};
    a.secret();
    return 0;
}
