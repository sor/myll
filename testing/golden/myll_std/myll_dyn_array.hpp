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
#include "myll_option.hpp"
namespace Myll
{
    template <typename T>
    class DynArray;
    constexpr std::uint64_t DefaultCapacity = 8;
    template <typename T>
    class DynArray
    {
    public:
        class Iterator;
        class Iterator
        {
            T* _ptr{};
        public:
            explicit Iterator(T* ptr)
            {
                _ptr = ptr;
            }
            inline Myll::DynArray<T>::Iterator& next()
            {
                ++_ptr;
                return (*this);
            }
            inline T& value() const
            {
                return *_ptr;
            }
            inline bool equals(Myll::DynArray<T>::Iterator other) const
            {
                return _ptr == other._ptr;
            }
            inline Myll::DynArray<T>::Iterator& operator++()
            {
                ++_ptr;
                return (*this);
            }
            inline T& operator*()
            {
                return *_ptr;
            }
            inline bool operator==(Myll::DynArray<T>::Iterator other) const
            {
                return _ptr == other._ptr;
            }
            inline bool operator!=(Myll::DynArray<T>::Iterator other) const
            {
                return _ptr != other._ptr;
            }
        };
    private:
        std::unique_ptr<T[]> _data{};
        std::uint64_t _size = 0;
        std::uint64_t _capacity = 0;
    public:
        DynArray()
        {
        }
        explicit DynArray(std::uint64_t initialCapacity)
        {
            _capacity = initialCapacity;
            if( initialCapacity > 0 ) {
                _data = std::make_unique<T[]>(initialCapacity);
            }
        }
        DynArray(const std::initializer_list<T>& init)
        {
            const T* it = init.begin();
            const T* last = init.end();
            reserve( static_cast<std::uint64_t>( (last - it) ) );
            while( it != last ) {
                pushBack( *it );
                ++it;
            }
        }
        DynArray(Myll::DynArray<T>&& other)
        {
            _data = std::move( other._data );
            _size = other._size;
            _capacity = other._capacity;
            other._data = nullptr;
            other._size = 0;
            other._capacity = 0;
        }
    private:
        inline void grow()
        {
            const std::uint64_t new_capacity = _capacity == 0 ? 1 : _capacity * 2;
            reserve( new_capacity );
        }
        inline void swap(std::uint64_t i, std::uint64_t j)
        {
            T tmp = _data[i];
            _data[i] = _data[j];
            _data[j] = tmp;
        }
        inline void sortRange(std::uint64_t left, std::uint64_t right)
        {
            if( left < right ) {
                std::uint64_t pivot = partition( left, right );
                if( pivot > 0 ) {
                    sortRange( left, pivot - 1 );
                }
                sortRange( pivot + 1, right );
            }
        }
        inline std::uint64_t partition(std::uint64_t left, std::uint64_t right)
        {
            T pivotValue = _data[right];
            std::uint64_t i = left;
            std::uint64_t j = left;
            while( j < right ) {
                if( _data[j] < pivotValue ) {
                    swap( i, j );
                    ++i;
                }
                ++j;
            }
            swap( i, right );
            return i;
        }
    public:
        inline Myll::DynArray<T>& operator=(Myll::DynArray<T>&& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            _data = std::move( other._data );
            _size = other._size;
            _capacity = other._capacity;
            other._data = nullptr;
            other._size = 0;
            other._capacity = 0;
            return (*this);
        }
        inline Myll::DynArray<T>& reserve(std::uint64_t newCapacity)
        {
            if( newCapacity > _capacity ) {
                std::unique_ptr<T[]> new_data = std::make_unique<T[]>(newCapacity);
                for( int i = 0; i < (_size)+0; ++i ) {
                    new_data[i] = _data[i];
                }
                _capacity = newCapacity;
                _data = std::move( new_data );
            }
            return (*this);
        }
        inline Myll::DynArray<T>& pushBack(const T& value)
        {
            if( _size >= _capacity ) {
                grow();
            }
            _data[_size] = value;
            ++_size;
            return (*this);
        }
        inline Myll::DynArray<T>& popBack()
        {
            --_size;
            return (*this);
        }
        inline Myll::DynArray<T>& clear()
        {
            _size = 0;
            return (*this);
        }
        inline Myll::DynArray<T>& shrinkToFit()
        {
            if( _capacity > _size ) {
                if( _size == 0 ) {
                    _data = nullptr;
                    _capacity = 0;
                } else {
                    std::unique_ptr<T[]> new_data = std::make_unique<T[]>(_size);
                    for( int i = 0; i < (_size)+0; ++i ) {
                        new_data[i] = _data[i];
                    }
                    _capacity = _size;
                    _data = std::move( new_data );
                }
            }
            return (*this);
        }
        constexpr inline std::uint64_t size() const
        {
            return _size;
        }
        constexpr inline std::uint64_t capacity() const
        {
            return _capacity;
        }
        constexpr inline bool isEmpty() const
        {
            return _size == 0;
        }
        inline T& at(std::uint64_t index)
        {
            return _data[index];
        }
        inline T& front()
        {
            return _data[0];
        }
        inline T& back()
        {
            return _data[_size - 1];
        }
        inline T& top()
        {
            return back();
        }
        inline T& operator[](std::uint64_t index)
        {
            return _data[index];
        }
        inline Myll::DynArray<T>& sort()
        {
            if( _size > 1 ) {
                sortRange( 0, _size - 1 );
            }
            return (*this);
        }
        inline Myll::DynArray<T>& reverse()
        {
            if( _size == 0 ) {
                return (*this);
            }
            std::uint64_t i = 0;
            std::uint64_t j = _size - 1;
            while( i < j ) {
                swap( i, j );
                ++i;
                --j;
            }
            return (*this);
        }
        inline Myll::DynArray<T>& fill(const T& value)
        {
            std::uint64_t i = 0;
            while( i < _size ) {
                _data[i] = value;
                ++i;
            }
            return (*this);
        }
        inline Myll::DynArray<T>& insert(std::uint64_t index, const T& value)
        {
            if( index > _size ) {
                index = _size;
            }
            if( _size >= _capacity ) {
                grow();
            }
            std::uint64_t i = _size;
            while( i > index ) {
                _data[i] = _data[i - 1];
                --i;
            }
            _data[index] = value;
            ++_size;
            return (*this);
        }
        inline Myll::DynArray<T>& erase(std::uint64_t index)
        {
            if( index >= _size ) {
                return (*this);
            }
            std::uint64_t i = index;
            while( i < _size - 1 ) {
                _data[i] = _data[i + 1];
                ++i;
            }
            --_size;
            return (*this);
        }
        inline bool remove(const T& value)
        {
            Myll::Option<std::uint64_t> first = indexOf( value );
            if( !first.has() ) {
                return false;
            }
            erase( first.value() );
            return true;
        }
        inline Myll::Option<std::uint64_t> indexOf(const T& value)
        {
            std::uint64_t i = 0;
            while( i < _size ) {
                if( _data[i] == value ) {
                    return Myll::Option<std::uint64_t>::some( i );
                }
                ++i;
            }
            return Myll::Option<std::uint64_t>::none();
        }
        inline bool contains(const T& value)
        {
            std::uint64_t i = 0;
            while( i < _size ) {
                if( _data[i] == value ) {
                    return true;
                }
                ++i;
            }
            return false;
        }
        inline Myll::DynArray<T>::Iterator find(const T& value)
        {
            std::uint64_t i = 0;
            while( i < _size ) {
                if( _data[i] == value ) {
                    return Iterator( &_data[i] );
                }
                ++i;
            }
            return end();
        }
        inline bool binarySearch(const T& value)
        {
            std::uint64_t left = 0;
            std::uint64_t right = _size;
            while( left < right ) {
                std::uint64_t mid = left + (right - left) / 2;
                if( _data[mid] < value ) {
                    left = mid + 1;
                } else if( value < _data[mid] ) {
                    right = mid;
                } else {
                    return true;
                }
            }
            return false;
        }
        inline std::uint64_t count(const T& value)
        {
            std::uint64_t result = 0;
            std::uint64_t i = 0;
            while( i < _size ) {
                if( _data[i] == value ) {
                    ++result;
                }
                ++i;
            }
            return result;
        }
        inline Myll::DynArray<T>::Iterator begin()
        {
            if( _size == 0 ) {
                return Iterator( nullptr );
            }
            return Iterator( &_data[0] );
        }
        inline Myll::DynArray<T>::Iterator end()
        {
            if( _size == 0 ) {
                return Iterator( nullptr );
            }
            return Iterator( &_data[_size] );
        }
        inline T* beginPtr()
        {
            if( _size == 0 ) {
                return nullptr;
            }
            return &_data[0];
        }
        inline T* endPtr()
        {
            if( _size == 0 ) {
                return nullptr;
            }
            return &_data[_size];
        }
    };
}
