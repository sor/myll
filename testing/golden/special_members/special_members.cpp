#include "special_members.hpp"
RuleZero::RuleZero()
{
}
RuleAnyBuffer::RuleAnyBuffer(const RuleAnyBuffer& other)
{
    _size = other._size;
    _data = std::make_unique<int[]>(_size);
    std::uint64_t i = 0;
    while( i < _size ) {
        _data[i] = other._data[i];
        ++i;
    }
}
RuleAnyBuffer& RuleAnyBuffer::operator=(const RuleAnyBuffer& other)
{
    if( &(*this) == &other ) {
        return (*this);
    }
    _size = other._size;
    _data = std::make_unique<int[]>(_size);
    std::uint64_t i = 0;
    while( i < _size ) {
        _data[i] = other._data[i];
        ++i;
    }
    return (*this);
}
RuleBuffer::RuleBuffer()
{
}
RuleBuffer::RuleBuffer(std::uint64_t size)
{
    _size = size;
    _data = std::make_unique<int[]>(size);
}
RuleBuffer::RuleBuffer(const RuleBuffer& other)
{
    _size = other._size;
    _data = std::make_unique<int[]>(_size);
    std::uint64_t i = 0;
    while( i < _size ) {
        _data[i] = other._data[i];
        ++i;
    }
}
RuleBuffer::~RuleBuffer()
{
}
RuleBuffer& RuleBuffer::operator=(const RuleBuffer& other)
{
    if( &(*this) == &other ) {
        return (*this);
    }
    _size = other._size;
    _data = std::make_unique<int[]>(_size);
    std::uint64_t i = 0;
    while( i < _size ) {
        _data[i] = other._data[i];
        ++i;
    }
    return (*this);
}
Buffer::Buffer()
{
}
Buffer::Buffer(std::uint64_t size)
{
    _size = size;
    _data = std::make_unique<int[]>(size);
}
Buffer::Buffer(const Buffer& other)
{
    _size = other._size;
    _data = std::make_unique<int[]>(_size);
    std::uint64_t i = 0;
    while( i < _size ) {
        _data[i] = other._data[i];
        ++i;
    }
}
Buffer::~Buffer()
{
}
Buffer& Buffer::operator=(const Buffer& other)
{
    if( &(*this) == &other ) {
        return (*this);
    }
    _size = other._size;
    _data = std::make_unique<int[]>(_size);
    std::uint64_t i = 0;
    while( i < _size ) {
        _data[i] = other._data[i];
        ++i;
    }
    return (*this);
}
Handle::Handle()
{
}
Handle::Handle(int value)
{
    _ptr = new int();
    *_ptr = value;
}
Handle::Handle(const Handle& other)
{
    _ptr = new int();
    *_ptr = *other._ptr;
}
Handle::Handle(Handle&& src)
{
    _ptr = src._ptr;
    src._ptr = nullptr;
}
Handle::~Handle()
{
}
Handle& Handle::operator=(const Handle& other)
{
    if( &(*this) == &other ) {
        return (*this);
    }
    _ptr = new int();
    *_ptr = *other._ptr;
    return (*this);
}
Handle& Handle::operator=(Handle&& src)
{
    if( &(*this) == &src ) {
        return (*this);
    }
    _ptr = src._ptr;
    src._ptr = nullptr;
    return (*this);
}
DefaultCopy::DefaultCopy()
{
}
DefaultCopy::~DefaultCopy()
{
}
DefaultCopy& DefaultCopy::operator=(const DefaultCopy& other)
{
    value = other.value;
    return (*this);
}
UBox::UBox()
{
}
UBox::UBox(std::uint64_t n)
{
    data = std::make_unique<int[]>(n);
}
UBox::UBox(UBox&& src)
{
    data = std::move( src.data );
    src.data = nullptr;
}
UBox& UBox::operator=(UBox&& src)
{
    if( &(*this) == &src ) {
        return (*this);
    }
    data = std::move( src.data );
    src.data = nullptr;
    return (*this);
}
PtrBox::PtrBox()
{
}
PtrBox::PtrBox(int value)
{
    ptr = std::make_unique<int>();
    *ptr = value;
}
PtrBox::PtrBox(PtrBox&& src)
{
    ptr = std::move( src.ptr );
    src.ptr = nullptr;
}
NamedBox::NamedBox()
{
}
NamedBox::NamedBox(int v)
{
    value = v;
}
NamedBox::NamedBox(const NamedBox& other)
{
    value = other.value;
}
NamedBox::NamedBox(NamedBox&& src)
{
    value = src.value;
}
NamedBox& NamedBox::operator=(const NamedBox& other)
{
    value = other.value;
    return (*this);
}
NamedBox& NamedBox::operator=(NamedBox&& src)
{
    value = src.value;
    return (*this);
}
DefaultBox::DefaultBox()
{
}
DefaultBox::DefaultBox(int v)
{
    value = v;
}
DefaultBox::DefaultBox(const DefaultBox& other)
{
    value = other.value;
}
DefaultBox::DefaultBox(DefaultBox&& other)
{
    value = other.value;
}
DefaultBox& DefaultBox::operator=(const DefaultBox& other)
{
    value = other.value;
    return (*this);
}
DefaultBox& DefaultBox::operator=(DefaultBox&& other)
{
    value = other.value;
    return (*this);
}
int main()
{
    if( run_rule_of_n() != 0 ) {
        return 1;
    }
    if( run_special_ctors() != 0 ) {
        return 2;
    }
    if( run_special_member_names() != 0 ) {
        return 3;
    }
    return 0;
}
int run_rule_of_n()
{
    RuleBuffer a = RuleBuffer( 3 );
    a._data[0] = 10;
    a._data[1] = 20;
    a._data[2] = 30;
    RuleBuffer b = a;
    if( b._size != 3 ) {
        return 1;
    }
    if( b._data[0] != 10 ) {
        return 2;
    }
    RuleBuffer c{};
    c = a;
    if( c._data[1] != 20 ) {
        return 3;
    }
    RuleZero z{};
    z.value = 42;
    if( z.value != 42 ) {
        return 4;
    }
    RuleAnyBuffer ab{};
    if( ab._size != 0 ) {
        return 5;
    }
    RuleAnyBuffer abCopy = ab;
    if( abCopy._size != 0 ) {
        return 6;
    }
    return 0;
}
int run_special_ctors()
{
    Buffer a = Buffer( 3 );
    a._data[0] = 10;
    Buffer b = a;
    if( b._size != 3 ) {
        return 1;
    }
    if( b._data[0] != 10 ) {
        return 2;
    }
    Buffer c{};
    c = a;
    if( c._data[0] != 10 ) {
        return 3;
    }
    Handle h = Handle( 42 );
    Handle i = h;
    if( *i._ptr != 42 ) {
        return 4;
    }
    Handle j{};
    j = i;
    if( *j._ptr != 42 ) {
        return 5;
    }
    Handle k = std::move( h );
    if( *k._ptr != 42 ) {
        return 6;
    }
    Handle m{};
    m = std::move( k );
    if( *m._ptr != 42 ) {
        return 7;
    }
    DefaultCopy d1{};
    d1.value = 7;
    DefaultCopy d2 = d1;
    if( d2.value != 7 ) {
        return 8;
    }
    int* rp = nullptr;
    rp = nullptr;
    std::unique_ptr<int> up = nullptr;
    up = nullptr;
    std::unique_ptr<int[]> ua = nullptr;
    ua = nullptr;
    UBox box = UBox( 4 );
    UBox movedBox{};
    movedBox = std::move( box );
    PtrBox pb = PtrBox( 7 );
    PtrBox qb = std::move( pb );
    if( *qb.ptr != 7 ) {
        return 9;
    }
    return 0;
}
int run_special_member_names()
{
    NamedBox a = NamedBox( 7 );
    NamedBox b = a;
    if( b.value != 7 ) {
        return 1;
    }
    NamedBox c = std::move( a );
    if( c.value != 7 ) {
        return 2;
    }
    NamedBox d{};
    d = b;
    if( d.value != 7 ) {
        return 3;
    }
    d = std::move( c );
    if( d.value != 7 ) {
        return 4;
    }
    DefaultBox e = DefaultBox( 8 );
    DefaultBox f = e;
    if( f.value != 8 ) {
        return 5;
    }
    DefaultBox g = std::move( e );
    if( g.value != 8 ) {
        return 6;
    }
    DefaultBox h{};
    h = f;
    if( h.value != 8 ) {
        return 7;
    }
    h = std::move( g );
    if( h.value != 8 ) {
        return 8;
    }
    NamedInitList<int> ni = { 1, 2, 3 };
    if( ni.sum != 6 ) {
        return 9;
    }
    DefaultInitList<int> di = { 1, 2, 3, 4 };
    if( di.sum != 10 ) {
        return 10;
    }
    return 0;
}
