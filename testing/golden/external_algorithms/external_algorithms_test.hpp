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
#include "external_algorithms.hpp"
namespace App
{
    template <typename T>
    class Simple;
    template <typename T>
    class Simple
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
            inline App::Simple<T>::Iterator& next()
            {
                ++_ptr;
                return (*this);
            }
            inline T& value() const
            {
                return *_ptr;
            }
            inline App::Simple<T>::Iterator& operator++()
            {
                ++_ptr;
                return (*this);
            }
            inline T& operator*()
            {
                return *_ptr;
            }
            inline bool operator==(App::Simple<T>::Iterator other) const
            {
                return _ptr == other._ptr;
            }
            inline bool operator!=(App::Simple<T>::Iterator other) const
            {
                return _ptr != other._ptr;
            }
        };
    private:
        std::unique_ptr<T[]> _data{};
        std::uint64_t _size = 0;
    public:
        std::uint64_t _capacity = 0;
        Simple()
        {
        }
    private:
        inline void grow()
        {
            const std::uint64_t new_capacity = _capacity == 0 ? 1 : _capacity * 2;
            std::unique_ptr<T[]> new_data = std::make_unique<T[]>(new_capacity);
            std::uint64_t i = 0;
            while( i < _size ) {
                new_data[i] = _data[i];
                ++i;
            }
            _capacity = new_capacity;
            _data = std::move( new_data );
        }
    public:
        inline App::Simple<T>& pushBack(const T& value)
        {
            if( _size >= _capacity ) {
                grow();
            }
            _data[_size] = value;
            ++_size;
            return (*this);
        }
        inline App::Simple<T>::Iterator begin()
        {
            if( _size == 0 ) {
                return Iterator( nullptr );
            }
            return Iterator( &_data[0] );
        }
        inline App::Simple<T>::Iterator end()
        {
            if( _size == 0 ) {
                return Iterator( nullptr );
            }
            return Iterator( &_data[_size] );
        }
        inline App::Simple<T>& sort()
        {
            Algo::sortRange<App::Simple<T>::Iterator>( begin(), end() );
            return (*this);
        }
        constexpr inline std::uint64_t size() const
        {
            return _size;
        }
        inline T& at(std::uint64_t index)
        {
            return _data[index];
        }
    };
}
