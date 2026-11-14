#include "std_lib.hpp"
Vec2::Vec2()
{
    x = 0;
    y = 0;
}
Vec2::Vec2(int ax, int ay)
{
    x = ax;
    y = ay;
}
bool Vec2::operator==(Vec2 other) const
{
    return x == other.x && y == other.y;
}
int runOption()
{
    Myll::Option<int> a = Myll::Option<int>::some( 41 );
    if( !a.has() ) {
        return 1;
    }
    if( a.value() != 41 ) {
        return 1;
    }
    a.put( 42 );
    if( a.valueOr( 7 ) != 42 ) {
        return 1;
    }
    a.reset();
    if( a.has() ) {
        return 1;
    }
    if( a.valueOr( 7 ) != 7 ) {
        return 1;
    }
    Myll::Option<int> b = Myll::Option<int>::none();
    if( b.has() ) {
        return 1;
    }
    Myll::Option<float> c( 1.5f );
    if( c.value() < 1.4f || c.value() > 1.6f ) {
        return 1;
    }
    Myll::Option<int> d( 1 );
    Myll::Option<int> e = d;
    if( e.value() != 1 ) {
        return 1;
    }
    Myll::Option<int> f = std::move( d );
    if( f.value() != 1 ) {
        return 1;
    }
    return 0;
}
Myll::Result<int, int> divide(int a, int b)
{
    if( b == 0 ) {
        return Myll::Result<int, int>::fail( 42 );
    }
    return Myll::Result<int, int>::ok( a / b );
}
int runResult()
{
    Myll::Result<int, int> r = divide( 10, 2 );
    if( !r.isOk() ) {
        return 1;
    }
    if( r.value() != 5 ) {
        return 1;
    }
    if( r.errorOr( 7 ) != 7 ) {
        return 1;
    }
    Myll::Result<int, int> f = divide( 1, 0 );
    if( !f.isFail() ) {
        return 1;
    }
    if( f.error() != 42 ) {
        return 1;
    }
    if( f.valueOr( 99 ) != 99 ) {
        return 1;
    }
    Myll::Result<float, int> m = Myll::Result<float, int>::ok( 1.5f );
    if( m.value() < 1.4f || m.value() > 1.6f ) {
        return 1;
    }
    Myll::Result<int, int> c = r;
    if( c.value() != 5 ) {
        return 1;
    }
    return 0;
}
int runRng()
{
    Myll::Rng r( 42 );
    std::uint64_t a = r.next();
    std::uint64_t b = r.next();
    if( a == b ) {
        return 1;
    }
    if( r.state() == 0 ) {
        return 1;
    }
    Myll::Rng r2( 42 );
    if( r2.next() != a ) {
        return 1;
    }
    std::uint64_t v = r.range( 0, 9 );
    if( v > 9 ) {
        return 1;
    }
    if( r.range( 5, 5 ) != 5 ) {
        return 1;
    }
    float f = r.nextF32();
    if( f < 0.0f || f >= 1.0f ) {
        return 1;
    }
    double g = r.nextF64();
    if( g < 0.0f || g >= 1.0f ) {
        return 1;
    }
    std::uint32_t u = r.nextU32();
    if( u == 0 && r.nextU32() == 0 ) {
        return 1;
    }
    return 0;
}
int runDynArray()
{
    Myll::DynArray<int> a{};
    a.pushBack( 5 ).pushBack( 6 ).pushBack( 7 );
    a.insert( 1, 9 );
    if( a[0] != 5 || a[1] != 9 || a[2] != 6 || a[3] != 7 ) {
        return 1;
    }
    a.erase( 2 );
    if( a.size() != 3 || a[2] != 7 ) {
        return 1;
    }
    if( !a.contains( 9 ) || a.contains( 42 ) ) {
        return 1;
    }
    Myll::Option<std::uint64_t> i1 = a.indexOf( 7 );
    if( !i1.has() || i1.value() != 2 ) {
        return 1;
    }
    Myll::Option<std::uint64_t> i2 = a.indexOf( 99 );
    if( i2.has() ) {
        return 1;
    }
    if( !a.remove( 5 ) || a.size() != 2 || a[0] != 9 ) {
        return 1;
    }
    if( a.remove( 55 ) ) {
        return 1;
    }
    a.insert( 99, 1 );
    if( a.size() != 3 || a[2] != 1 ) {
        return 1;
    }
    return 0;
}
int runString()
{
    Myll::String s( "hello" );
    if( s.len() != 5 ) {
        return 1;
    }
    if( s.isEmpty() ) {
        return 1;
    }
    if( s[0] != 'h' || s.at( 4 ) != 'o' ) {
        return 1;
    }
    s.pushBack( '!' );
    if( s.len() != 6 ) {
        return 1;
    }
    Myll::String t( " world" );
    s.append( t.view() );
    if( s.len() != 12 ) {
        return 1;
    }
    if( !s.contains( "wor" ) ) {
        return 1;
    }
    if( s.contains( "xyz" ) ) {
        return 1;
    }
    if( !s.startsWith( "hello" ) ) {
        return 1;
    }
    if( !s.endsWith( "world" ) ) {
        return 1;
    }
    Myll::Option<std::uint64_t> i1 = s.find( 'o' );
    if( !i1.has() || i1.value() != 4 ) {
        return 1;
    }
    Myll::Option<std::uint64_t> i2 = s.find( "world" );
    if( !i2.has() || i2.value() != 7 ) {
        return 1;
    }
    Myll::String sub = s.substring( 7, 12 );
    if( sub.len() != 5 ) {
        return 1;
    }
    if( !(sub == "world") ) {
        return 1;
    }
    Myll::String copyOf = s;
    if( copyOf.len() != 12 ) {
        return 1;
    }
    copyOf.pushBack( 'x' );
    if( copyOf.len() != 13 ) {
        return 1;
    }
    if( s.len() != 12 ) {
        return 1;
    }
    Myll::String moved = std::move( s );
    if( moved.len() != 12 ) {
        return 1;
    }
    Myll::String a( "abc" );
    Myll::String b( "abd" );
    if( !(a < b) ) {
        return 1;
    }
    if( a == b ) {
        return 1;
    }
    if( !(a != b) ) {
        return 1;
    }
    Myll::StringView v( "xyz" );
    if( v.len() != 3 ) {
        return 1;
    }
    if( !v.contains( "y" ) ) {
        return 1;
    }
    if( !(Myll::String( "same" ) == "same") ) {
        return 1;
    }
    return 0;
}
int runMap()
{
    Myll::Map<int, int> m{};
    m.put( 1, 10 ).put( 2, 20 ).put( 3, 30 );
    if( m.size() != 3 ) {
        return 1;
    }
    if( !m.has( 2 ) || m.has( 99 ) ) {
        return 1;
    }
    int* p = m.at( 1 );
    if( p == nullptr || *p != 10 ) {
        return 1;
    }
    if( m.at( 77 ) != nullptr ) {
        return 1;
    }
    if( m.getOr( 2, 5 ) != 20 ) {
        return 1;
    }
    if( m.getOr( 77, 5 ) != 5 ) {
        return 1;
    }
    m.put( 1, 11 );
    if( m.size() != 3 || m.getOr( 1, 0 ) != 11 ) {
        return 1;
    }
    if( !m.remove( 2 ) ) {
        return 1;
    }
    if( m.has( 2 ) || m.size() != 2 ) {
        return 1;
    }
    if( m.remove( 2 ) ) {
        return 1;
    }
    std::uint64_t i = 0;
    while( i < 100 ) {
        m.put( static_cast<int>( i ), static_cast<int>( (i * 7) ) );
        ++i;
    }
    if( m.size() != 100 ) {
        return 1;
    }
    i = 0;
    while( i < 100 ) {
        if( m.getOr( static_cast<int>( i ), 0 ) != i * 7 ) {
            return 1;
        }
        ++i;
    }
    i = 0;
    while( i < 100 ) {
        if( ((i) & 1) == 0 ) {
            m.remove( static_cast<int>( i ) );
        }
        ++i;
    }
    if( m.size() != 50 ) {
        return 1;
    }
    i = 0;
    while( i < 100 ) {
        if( ((i) & 1) == 0 && m.has( static_cast<int>( i ) ) ) {
            return 1;
        }
        if( ((i) & 1) != 0 && !m.has( static_cast<int>( i ) ) ) {
            return 1;
        }
        ++i;
    }
    i = 0;
    while( i < 100 ) {
        m.put( static_cast<int>( i ), static_cast<int>( i ) );
        ++i;
    }
    if( m.size() != 100 ) {
        return 1;
    }
    Myll::DynArray<int> ks = m.keys();
    if( ks.size() != 100 ) {
        return 1;
    }
    Myll::DynArray<int> vs = m.values();
    if( vs.size() != 100 ) {
        return 1;
    }
    Myll::Map<int, int> mm = m;
    if( mm.size() != 100 ) {
        return 1;
    }
    Myll::Map<int, int> mv = std::move( m );
    if( mv.size() != 100 ) {
        return 1;
    }
    Myll::Map<Myll::String, int> ms{};
    ms.put( "abc", 1 ).put( "abd", 2 ).put( "xyz", 3 );
    if( ms.size() != 3 ) {
        return 1;
    }
    if( !ms.has( "abc" ) ) {
        return 1;
    }
    if( ms.getOr( "abd", 0 ) != 2 ) {
        return 1;
    }
    if( ms.has( "nope" ) ) {
        return 1;
    }
    if( !ms.remove( "abd" ) || ms.has( "abd" ) ) {
        return 1;
    }
    std::uint64_t k = 0;
    while( k < 60 ) {
        Myll::String key( "k" );
        key.append( "000000" );
        Myll::String tail{};
        tail.pushBack( static_cast<char>( ('0' + k / 10) ) );
        tail.pushBack( static_cast<char>( ('0' + k % 10) ) );
        key.append( tail.view() );
        ms.put( key, static_cast<int>( k ) );
        ++k;
    }
    if( ms.size() != 62 ) {
        return 1;
    }
    return 0;
}
std::uint64_t hash(Vec2 v)
{
    return static_cast<std::uint64_t>( (v.x * 73856093 + v.y * 19349663) );
}
int runSet()
{
    Myll::Set<int> s{};
    if( !s.add( 1 ) ) {
        return 1;
    }
    if( s.add( 1 ) ) {
        return 1;
    }
    if( !s.add( 2 ) || !s.add( 3 ) ) {
        return 1;
    }
    if( s.size() != 3 ) {
        return 1;
    }
    if( !s.has( 2 ) || s.has( 99 ) ) {
        return 1;
    }
    if( !s.remove( 2 ) ) {
        return 1;
    }
    if( s.has( 2 ) || s.size() != 2 ) {
        return 1;
    }
    if( s.remove( 2 ) ) {
        return 1;
    }
    std::uint64_t i = 0;
    while( i < 100 ) {
        s.add( static_cast<int>( i ) );
        ++i;
    }
    if( s.size() != 100 ) {
        return 1;
    }
    i = 0;
    while( i < 50 ) {
        s.remove( static_cast<int>( i ) );
        ++i;
    }
    if( s.size() != 50 ) {
        return 1;
    }
    i = 0;
    while( i < 100 ) {
        if( i < 50 && s.has( static_cast<int>( i ) ) ) {
            return 1;
        }
        if( i >= 50 && !s.has( static_cast<int>( i ) ) ) {
            return 1;
        }
        ++i;
    }
    Myll::DynArray<int> vals = s.values();
    if( vals.size() != 50 ) {
        return 1;
    }
    Myll::Set<int> sc = s;
    if( sc.size() != 50 ) {
        return 1;
    }
    Myll::Set<int> sm = std::move( s );
    if( sm.size() != 50 ) {
        return 1;
    }
    Myll::Map<Vec2, int> vm{};
    vm.put( Vec2( 1, 2 ), 10 );
    vm.put( Vec2( 3, 4 ), 30 );
    if( vm.size() != 2 ) {
        return 1;
    }
    if( vm.getOr( Vec2( 1, 2 ), 0 ) != 10 ) {
        return 1;
    }
    if( !vm.remove( Vec2( 3, 4 ) ) ) {
        return 1;
    }
    if( vm.has( Vec2( 3, 4 ) ) ) {
        return 1;
    }
    return 0;
}
int main()
{
    if( runOption() != 0 ) {
        return 1;
    }
    if( runResult() != 0 ) {
        return 2;
    }
    if( runRng() != 0 ) {
        return 3;
    }
    if( runDynArray() != 0 ) {
        return 4;
    }
    if( runString() != 0 ) {
        return 5;
    }
    if( runMap() != 0 ) {
        return 6;
    }
    if( runSet() != 0 ) {
        return 7;
    }
    return 0;
}
