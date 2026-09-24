#include "main.hpp"
void PrivateClass::secret() const
{
}
MutNum::MutNum(int value)
{
    v = value;
}
MutNum MutNum::operator+(const MutNum& other) const
{
    v = v + other.v;
    return MutNum( v );
}
int test_private_call()
{
    PrivateClass a{};
    a.secret();
    return 0;
}
int test_const_operator()
{
    MutNum a = MutNum( 1 );
    MutNum b = MutNum( 2 );
    MutNum c = a + b;
    return 0;
}
int main()
{
    return 0;
}
