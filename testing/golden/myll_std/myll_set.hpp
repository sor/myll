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
    template <typename K>
    class Set;
    template <typename K>
    class Set
    {
        std::unique_ptr<K[]> _keys{};
        std::unique_ptr<std::uint8_t[]> _state{};
        std::uint64_t _size = 0;
        std::uint64_t _tomb = 0;
        std::uint64_t _capacity = 0;
    public:
        Set() = default;
        explicit Set(std::uint64_t initialCapacity)
        {
            if( initialCapacity < 8 ) {
                initialCapacity = 8;
            }
            rehashTo( nextPowerOfTwo( initialCapacity ) );
        }
        Set(const Myll::Set<K>& other)
        {
            rehashTo( other._capacity );
            std::uint64_t slot = 0;
            while( slot < other._capacity ) {
                if( other._state[slot] == 1 ) {
                    add( other._keys[slot] );
                }
                ++slot;
            }
        }
        Set(Myll::Set<K>&& other)
        {
            _keys = std::move( other._keys );
            _state = std::move( other._state );
            _size = other._size;
            _tomb = other._tomb;
            _capacity = other._capacity;
            other._size = 0;
            other._tomb = 0;
            other._capacity = 0;
        }
        ~Set() = default;
    private:
        inline void rehashTo(std::uint64_t newCapacity)
        {
            std::unique_ptr<K[]> oldKeys = std::move( _keys );
            std::unique_ptr<std::uint8_t[]> oldState = std::move( _state );
            std::uint64_t oldCap = _capacity;
            _keys = std::make_unique<K[]>(newCapacity);
            _state = std::make_unique<std::uint8_t[]>(newCapacity);
            _capacity = newCapacity;
            _size = 0;
            _tomb = 0;
            std::uint64_t slot = 0;
            while( slot < oldCap ) {
                if( oldState[slot] == 1 ) {
                    add( oldKeys[slot] );
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
        inline Myll::Set<K>& operator=(const Myll::Set<K>& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            clear();
            std::uint64_t slot = 0;
            while( slot < other._capacity ) {
                if( other._state[slot] == 1 ) {
                    add( other._keys[slot] );
                }
                ++slot;
            }
            return (*this);
        }
        inline Myll::Set<K>& operator=(Myll::Set<K>&& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            _keys = std::move( other._keys );
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
        inline Myll::Set<K>& clear()
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
        inline bool add(K key)
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
                    return false;
                }
                slot = (slot + 1) & (_capacity - 1);
            }
            if( _state[slot] == 2 ) {
                --_tomb;
            }
            _state[slot] = 1;
            _keys[slot] = key;
            ++_size;
            return true;
        }
        inline bool has(K key)
        {
            if( _capacity == 0 ) {
                return false;
            }
            std::uint64_t h = hash( key );
            std::uint64_t slot = static_cast<std::uint64_t>( ((h) & static_cast<std::uint64_t>( (_capacity - 1) )) );
            while( _state[slot] != 0 ) {
                if( _state[slot] == 1 && _keys[slot] == key ) {
                    return true;
                }
                slot = (slot + 1) & (_capacity - 1);
            }
            return false;
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
        inline typename Myll::DynArray<K> values()
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
    };
}
