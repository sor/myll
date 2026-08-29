#include "Box.hpp"
void Box::updatePriv(std::int32_t a, std::int32_t b, std::int32_t c)
{
    _x = a;
    _y = b;
    _z = c;
}
std::int32_t Box::helperA()
{
    return _x + _y;
}
std::int32_t Box::helperB()
{
    return _y + _z;
}
std::int32_t Box::sum()
{
    return helperA() + helperB();
}
std::int32_t Box::getX()
{
    return _x;
}
void Box::update(std::int32_t a, std::int32_t b, std::int32_t c)
{
    updatePriv( a, b, c );
}
std::int32_t Box::volume()
{
    return _x * _y * _z;
}
std::int32_t Box::getY()
{
    return _y;
}
std::int32_t Box::getZ()
{
    return _z;
}
void Box::setSecret(std::int32_t v)
{
    _secret = v;
}
std::int32_t Box::getSecret()
{
    return _secret;
}
void Box::setChildVisible(std::int32_t v)
{
    _childVisible = v;
}
std::int32_t Box::getChildVisible()
{
    return _childVisible;
}
std::int32_t Box::getSum()
{
    return sum();
}
