#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <initializer_list>
#include <memory>
#include <string>
#include <type_traits>
#include <utility>
#include "myll_dyn_array.hpp"
#include "myll_hash.hpp"
namespace Myll
{
    template <typename K, typename V>
    class Map;
    template <typename K, typename V>
    class Map
    {
        std::unique_ptr<K[]> _keys{};
        std::unique_ptr<V[]> _values{};
        std::unique_ptr<std::uint8_t[]> _state{};
        std::uint64_t _size = 0;
        std::uint64_t _tomb = 0;
        std::uint64_t _capacity = 0;
    public:
        Map() = default;
        explicit Map(std::uint64_t initialCapacity)
        {
            if( initialCapacity < 8 ) {
                initialCapacity = 8;
            }
            rehashTo( nextPowerOfTwo( initialCapacity ) );
        }
        Map(const Myll::Map<K, V>& other)
        {
            rehashTo( other._capacity );
            std::uint64_t slot = 0;
            while( slot < other._capacity ) {
                if( other._state[slot] == 1 ) {
                    put( other._keys[slot], other._values[slot] );
                }
                ++slot;
            }
        }
        Map(Myll::Map<K, V>&& other)
        {
            _keys = std::move( other._keys );
            _values = std::move( other._values );
            _state = std::move( other._state );
            _size = other._size;
            _tomb = other._tomb;
            _capacity = other._capacity;
            other._size = 0;
            other._tomb = 0;
            other._capacity = 0;
        }
        ~Map() = default;
    private:
        inline void rehashTo(std::uint64_t newCapacity)
        {
            std::unique_ptr<K[]> oldKeys = std::move( _keys );
            std::unique_ptr<V[]> oldValues = std::move( _values );
            std::unique_ptr<std::uint8_t[]> oldState = std::move( _state );
            std::uint64_t oldCap = _capacity;
            _keys = std::make_unique<K[]>(newCapacity);
            _values = std::make_unique<V[]>(newCapacity);
            _state = std::make_unique<std::uint8_t[]>(newCapacity);
            _capacity = newCapacity;
            _size = 0;
            _tomb = 0;
            std::uint64_t slot = 0;
            while( slot < oldCap ) {
                if( oldState[slot] == 1 ) {
                    put( oldKeys[slot], oldValues[slot] );
                }
                ++slot;
            }
        }
        inline std::uint64_t nextPowerOfTwo(std::uint64_t value)
        {
            std::uint64_t result = 8;
            while( result < value ) {
                result = result * 2;
            }
            return result;
        }
    public:
        inline Myll::Map<K, V>& operator=(const Myll::Map<K, V>& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            clear();
            std::uint64_t slot = 0;
            while( slot < other._capacity ) {
                if( other._state[slot] == 1 ) {
                    put( other._keys[slot], other._values[slot] );
                }
                ++slot;
            }
            return (*this);
        }
        inline Myll::Map<K, V>& operator=(Myll::Map<K, V>&& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            _keys = std::move( other._keys );
            _values = std::move( other._values );
            _state = std::move( other._state );
            _size = other._size;
            _tomb = other._tomb;
            _capacity = other._capacity;
            other._size = 0;
            other._tomb = 0;
            other._capacity = 0;
            return (*this);
        }
        inline std::uint64_t size() const
        {
            return _size;
        }
        inline bool isEmpty() const
        {
            return _size == 0;
        }
        inline Myll::Map<K, V>& clear()
        {
            std::uint64_t slot = 0;
            while( slot < _capacity ) {
                _state[slot] = 0;
                ++slot;
            }
            _size = 0;
            _tomb = 0;
            return (*this);
        }
        inline Myll::Map<K, V>& put(K key, V value)
        {
            if( _capacity == 0 ) {
                rehashTo( 8 );
            } else if( (_size + _tomb) * 4 >= _capacity * 3 ) {
                rehashTo( _capacity * 2 );
            }
            std::uint64_t h = hash( key );
            std::uint64_t slot = static_cast<std::uint64_t>( ((h) & static_cast<std::uint64_t>( (_capacity - 1) )) );
            while( _state[slot] == 1 ) {
                if( _keys[slot] == key ) {
                    _values[slot] = value;
                    return (*this);
                }
                slot = (slot + 1) & (_capacity - 1);
            }
            if( _state[slot] == 2 ) {
                --_tomb;
            }
            _state[slot] = 1;
            _keys[slot] = key;
            _values[slot] = value;
            ++_size;
            return (*this);
        }
        inline V* at(K key)
        {
            if( _capacity == 0 ) {
                return nullptr;
            }
            std::uint64_t h = hash( key );
            std::uint64_t slot = static_cast<std::uint64_t>( ((h) & static_cast<std::uint64_t>( (_capacity - 1) )) );
            while( _state[slot] != 0 ) {
                if( _state[slot] == 1 && _keys[slot] == key ) {
                    return &_values[slot];
                }
                slot = (slot + 1) & (_capacity - 1);
            }
            return nullptr;
        }
        inline V getOr(K key, V fallback)
        {
            V* found = at( key );
            if( found != nullptr ) {
                return *found;
            }
            return fallback;
        }
        inline bool has(K key)
        {
            return at( key ) != nullptr;
        }
        inline bool remove(K key)
        {
            if( _capacity == 0 ) {
                return false;
            }
            std::uint64_t h = hash( key );
            std::uint64_t slot = static_cast<std::uint64_t>( ((h) & static_cast<std::uint64_t>( (_capacity - 1) )) );
            while( _state[slot] != 0 ) {
                if( _state[slot] == 1 && _keys[slot] == key ) {
                    _state[slot] = 2;
                    --_size;
                    ++_tomb;
                    return true;
                }
                slot = (slot + 1) & (_capacity - 1);
            }
            return false;
        }
        inline typename Myll::DynArray<K> keys()
        {
            typename Myll::DynArray<K> result{};
            std::uint64_t slot = 0;
            while( slot < _capacity ) {
                if( _state[slot] == 1 ) {
                    result.pushBack( _keys[slot] );
                }
                ++slot;
            }
            return result;
        }
        inline typename Myll::DynArray<V> values()
        {
            typename Myll::DynArray<V> result{};
            std::uint64_t slot = 0;
            while( slot < _capacity ) {
                if( _state[slot] == 1 ) {
                    result.pushBack( _values[slot] );
                }
                ++slot;
            }
            return result;
        }
    };
}
