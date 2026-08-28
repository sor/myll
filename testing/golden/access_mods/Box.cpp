#include "Box.hpp"
void Box::updatePriv(int a, int b, int c)
{
    _x = a;
    _y = b;
    _z = c;
}
int Box::helperA()
{
    return _x + _y;
}
int Box::helperB()
{
    return _y + _z;
}
int Box::sum()
{
    return helperA() + helperB();
}
int Box::getX()
{
    return _x;
}
void Box::update(int a, int b, int c)
{
    updatePriv( a, b, c );
}
int Box::volume()
{
    return _x * _y * _z;
}
int Box::getY()
{
    return _y;
}
int Box::getZ()
{
    return _z;
}
void Box::setSecret(int v)
{
    _secret = v;
}
int Box::getSecret()
{
    return _secret;
}
void Box::setChildVisible(int v)
{
    _childVisible = v;
}
int Box::getChildVisible()
{
    return _childVisible;
}
int Box::getSum()
{
    return sum();
}
