#include "comparison.hpp"
bool AllOldWidget::operator==(const AllOldWidget& other) const
{
    return value == other.value;
}
bool AllOldWidget::operator!=(const AllOldWidget& other) const
{
    return value != other.value;
}
bool AllOldWidget::operator<(const AllOldWidget& other) const
{
    return value < other.value;
}
bool AllOldWidget::operator>(const AllOldWidget& other) const
{
    return value > other.value;
}
bool AllOldWidget::operator<=(const AllOldWidget& other) const
{
    return value <= other.value;
}
bool AllOldWidget::operator>=(const AllOldWidget& other) const
{
    return value >= other.value;
}
bool MaybeSpaceshipWidget::operator==(const MaybeSpaceshipWidget& other) const
{
    return value == other.value;
}
bool operator==(const Point& a, const Point& b)
{
    return a.x == b.x && a.y == b.y;
}
bool operator!=(const Point& a, const Point& b)
{
    return a.x != b.x || a.y != b.y;
}
int main()
{
    SpaceshipWidget a{};
    a.value = 1;
    SpaceshipWidget b{};
    b.value = 1;
    SpaceshipWidget c{};
    c.value = 2;
    if( !(a == b) ) {
        return 1;
    }
    if( a == c ) {
        return 2;
    }
    if( a != b ) {
        return 3;
    }
    if( !(a < c) ) {
        return 4;
    }
    if( !(a <= b) ) {
        return 5;
    }
    if( !(a >= b) ) {
        return 6;
    }
    if( !(c > a) ) {
        return 7;
    }
    MaybeSpaceshipWidget m1{};
    m1.value = 5;
    MaybeSpaceshipWidget m2{};
    m2.value = 5;
    if( !(m1 == m2) ) {
        return 8;
    }
    if( m1 != m2 ) {
        return 9;
    }
    AllOldWidget o1{};
    o1.value = 1;
    AllOldWidget o2{};
    o2.value = 2;
    if( o1 == o2 ) {
        return 10;
    }
    if( !(o1 != o2) ) {
        return 11;
    }
    if( !(o1 < o2) ) {
        return 12;
    }
    if( !(o1 <= o2) ) {
        return 13;
    }
    if( o1 > o2 ) {
        return 14;
    }
    if( o1 >= o2 ) {
        return 15;
    }
    AllNewWidget n1{};
    n1.value = 1;
    AllNewWidget n2{};
    n2.value = 1;
    if( !(n1 == n2) ) {
        return 16;
    }
    if( n1 != n2 ) {
        return 17;
    }
    Point p{};
    p.x = 1;
    p.y = 2;
    Point q{};
    q.x = 1;
    q.y = 2;
    Point r{};
    r.x = 2;
    r.y = 2;
    if( !(p == q) ) {
        return 18;
    }
    if( p == r ) {
        return 19;
    }
    if( p != q ) {
        return 20;
    }
    MaybeNothingWidget empty{};
    empty.value = 0;
    return 0;
}
