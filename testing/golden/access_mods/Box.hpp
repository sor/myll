#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
class Box;
class Box
{
    int _x = 0;
    int _y = 0;
    int _z = 0;
    int _tag;
public:
    const int maxSize = 100;
private:
    int _secret;
protected:
    int _childVisible;
private:
    void updatePriv(int a, int b, int c);
    int helperA();
    int helperB();
protected:
    int sum();
public:
    int getX();
    void update(int a, int b, int c);
    int volume();
    int getY();
    int getZ();
    void setSecret(int v);
    int getSecret();
    void setChildVisible(int v);
    int getChildVisible();
    int getSum();
};
