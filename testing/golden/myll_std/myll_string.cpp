#include "myll_string.hpp"
Myll::StringView::StringView(const char* other)
{
    _data = static_cast<const char*>( other );
    _len = other == nullptr ? 0 : std::strlen( other );
}
Myll::StringView::StringView(const char* data, std::uint64_t len)
{
    _data = static_cast<const char*>( data );
    _len = len;
}
std::uint64_t Myll::StringView::len() const
{
    return _len;
}
bool Myll::StringView::isEmpty() const
{
    return _len == 0;
}
const char& Myll::StringView::at(std::uint64_t index) const
{
    return _data[index];
}
const char& Myll::StringView::operator[](std::uint64_t index) const
{
    return _data[index];
}
bool Myll::StringView::equals(Myll::StringView other) const
{
    if( _len != other._len ) {
        return false;
    }
    std::uint64_t i = 0;
    while( i < _len ) {
        if( _data[i] != other._data[i] ) {
            return false;
        }
        ++i;
    }
    return true;
}
bool Myll::StringView::startsWith(Myll::StringView other) const
{
    if( other._len > _len ) {
        return false;
    }
    std::uint64_t i = 0;
    while( i < other._len ) {
        if( _data[i] != other._data[i] ) {
            return false;
        }
        ++i;
    }
    return true;
}
bool Myll::StringView::endsWith(Myll::StringView other) const
{
    if( other._len > _len ) {
        return false;
    }
    std::uint64_t i = 0;
    while( i < other._len ) {
        if( _data[_len - other._len + i] != other._data[i] ) {
            return false;
        }
        ++i;
    }
    return true;
}
bool Myll::StringView::contains(Myll::StringView other) const
{
    if( other._len == 0 ) {
        return true;
    }
    if( other._len > _len ) {
        return false;
    }
    std::uint64_t i = 0;
    while( i <= _len - other._len ) {
        bool same = true;
        std::uint64_t j = 0;
        while( j < other._len ) {
            if( _data[i + j] != other._data[j] ) {
                same = false;
                break;
            }
            ++j;
        }
        if( same ) {
            return true;
        }
        ++i;
    }
    return false;
}
bool Myll::StringView::contains(const char* cstr) const
{
    return contains( StringView( cstr ) );
}
Myll::String::String(Myll::StringView view)
{
    reserve( view.len() );
    for( int i = 0; i < (view.len())+0; ++i ) {
        pushBack( view.at( i ) );
    }
}
Myll::String::String(const char* other)
{
    append( Myll::StringView( other ) );
}
Myll::String::String(const Myll::String& other)
{
    reserve( other._len );
    for( int i = 0; i < (other._len)+0; ++i ) {
        pushBack( other._data[i] );
    }
}
Myll::String::String(Myll::String&& other)
{
    _data = std::move( other._data );
    _len = other._len;
    _capacity = other._capacity;
    other._data = nullptr;
    other._len = 0;
    other._capacity = 0;
}
void Myll::String::grow()
{
    reserve( _capacity == 0 ? 16 : _capacity * 2 );
}
Myll::String& Myll::String::operator=(Myll::String&& other)
{
    if( &(*this) == &other ) {
        return (*this);
    }
    _data = std::move( other._data );
    _len = other._len;
    _capacity = other._capacity;
    other._data = nullptr;
    other._len = 0;
    other._capacity = 0;
    return (*this);
}
Myll::String& Myll::String::operator=(const Myll::String& other)
{
    if( &(*this) == &other ) {
        return (*this);
    }
    clear();
    reserve( other._len );
    for( int i = 0; i < (other._len)+0; ++i ) {
        pushBack( other._data[i] );
    }
    return (*this);
}
std::uint64_t Myll::String::len() const
{
    return _len;
}
bool Myll::String::isEmpty() const
{
    return _len == 0;
}
char& Myll::String::at(std::uint64_t index)
{
    return _data[index];
}
char& Myll::String::operator[](std::uint64_t index)
{
    return _data[index];
}
Myll::String& Myll::String::reserve(std::uint64_t newCapacity)
{
    if( newCapacity > _capacity ) {
        std::unique_ptr<char[]> newData = std::make_unique<char[]>(newCapacity);
        for( int i = 0; i < (_len)+0; ++i ) {
            newData[i] = _data[i];
        }
        _capacity = newCapacity;
        _data = std::move( newData );
    }
    return (*this);
}
Myll::String& Myll::String::pushBack(char value)
{
    if( _len >= _capacity ) {
        grow();
    }
    _data[_len] = value;
    ++_len;
    return (*this);
}
Myll::String& Myll::String::append(Myll::StringView view)
{
    for( int i = 0; i < (view.len())+0; ++i ) {
        pushBack( view.at( i ) );
    }
    return (*this);
}
Myll::String& Myll::String::append(const char* cstr)
{
    append( Myll::StringView( cstr ) );
    return (*this);
}
Myll::String& Myll::String::append(Myll::String other)
{
    append( other.view() );
    return (*this);
}
Myll::String& Myll::String::appendRepeat(char value, std::uint64_t count)
{
    for( int myll_tmp_0 = 0; myll_tmp_0 < (count)+0; ++myll_tmp_0 ) {
        pushBack( value );
    }
    return (*this);
}
Myll::String& Myll::String::clear()
{
    _len = 0;
    return (*this);
}
Myll::StringView Myll::String::view() const
{
    if( _len == 0 ) {
        return Myll::StringView( nullptr, 0 );
    }
    return Myll::StringView( &_data[0], _len );
}
Myll::Option<std::uint64_t> Myll::String::find(char value)
{
    std::uint64_t i = 0;
    while( i < _len ) {
        if( _data[i] == value ) {
            return Myll::Option<std::uint64_t>::some( i );
        }
        ++i;
    }
    return Myll::Option<std::uint64_t>::none();
}
Myll::Option<std::uint64_t> Myll::String::find(Myll::StringView view)
{
    if( view.len() == 0 ) {
        return Myll::Option<std::uint64_t>::some( 0 );
    }
    if( view.len() > _len ) {
        return Myll::Option<std::uint64_t>::none();
    }
    std::uint64_t i = 0;
    while( i <= _len - view.len() ) {
        bool same = true;
        for( int j = 0; j < (view.len())+0; ++j ) {
            if( _data[i + j] != view.at( j ) ) {
                same = false;
                break;
            }
        }
        if( same ) {
            return Myll::Option<std::uint64_t>::some( i );
        }
        ++i;
    }
    return Myll::Option<std::uint64_t>::none();
}
Myll::Option<std::uint64_t> Myll::String::find(const char* cstr)
{
    return find( Myll::StringView( cstr ) );
}
bool Myll::String::contains(char value)
{
    Myll::Option<std::uint64_t> hit = find( value );
    return hit.has();
}
bool Myll::String::contains(Myll::StringView view)
{
    Myll::Option<std::uint64_t> hit = find( view );
    return hit.has();
}
bool Myll::String::contains(const char* cstr)
{
    return contains( Myll::StringView( cstr ) );
}
bool Myll::String::startsWith(Myll::StringView prefix)
{
    return view().startsWith( prefix );
}
bool Myll::String::startsWith(const char* cstr)
{
    return view().startsWith( Myll::StringView( cstr ) );
}
bool Myll::String::endsWith(Myll::StringView suffix)
{
    return view().endsWith( suffix );
}
bool Myll::String::endsWith(const char* cstr)
{
    return view().endsWith( Myll::StringView( cstr ) );
}
Myll::String Myll::String::substring(std::uint64_t start, std::uint64_t end)
{
    if( end > _len ) {
        end = _len;
    }
    if( start > end ) {
        start = end;
    }
    Myll::String result{};
    for( int i = 0; i < (end - start)+0; ++i ) {
        result.pushBack( _data[start + i] );
    }
    return result;
}
bool Myll::String::operator==(Myll::StringView other) const
{
    if( _len != other.len() ) {
        return false;
    }
    for( int i = 0; i < (_len)+0; ++i ) {
        if( _data[i] != other.at( i ) ) {
            return false;
        }
    }
    return true;
}
bool Myll::String::operator==(const char* cstr) const
{
    return (*this) == Myll::StringView( cstr );
}
bool Myll::String::operator==(const Myll::String& other) const
{
    return (*this) == other.view();
}
bool Myll::String::operator!=(Myll::StringView other) const
{
    return !((*this) == other);
}
bool Myll::String::operator!=(const char* cstr) const
{
    return !((*this) == Myll::StringView( cstr ));
}
bool Myll::String::operator!=(const Myll::String& other) const
{
    return !((*this) == other.view());
}
bool Myll::String::operator<(Myll::StringView other) const
{
    std::uint64_t shortest = _len < other.len() ? _len : other.len();
    for( int i = 0; i < (shortest)+0; ++i ) {
        if( _data[i] < other.at( i ) ) {
            return true;
        }
        if( _data[i] > other.at( i ) ) {
            return false;
        }
    }
    return _len < other.len();
}
bool Myll::String::operator<(const char* cstr) const
{
    return (*this) < Myll::StringView( cstr );
}
bool Myll::String::operator<(const Myll::String& other) const
{
    return (*this) < other.view();
}
