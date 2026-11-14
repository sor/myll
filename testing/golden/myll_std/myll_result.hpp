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
namespace Myll
{
    template <typename T, typename E>
    class Result;
    template <typename T, typename E>
    class Result
    {
    public:
        static inline Myll::Result<T, E> ok(T val)
        {
            Myll::Result<T, E> result{};
            result._value = val;
            result._ok = true;
            return result;
        }
        static inline Myll::Result<T, E> fail(E err)
        {
            Myll::Result<T, E> result{};
            result._error = err;
            result._ok = false;
            return result;
        }
    private:
        T _value{};
        E _error{};
        bool _ok = false;
    public:
        Result()
        {
        }
        Result(const Myll::Result<T, E>& other)
        {
            _value = other._value;
            _error = other._error;
            _ok = other._ok;
        }
        Result(Myll::Result<T, E>&& other)
        {
            _value = std::move( other._value );
            _error = std::move( other._error );
            _ok = other._ok;
            other._ok = false;
        }
        inline Myll::Result<T, E>& operator=(Myll::Result<T, E>&& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            _value = std::move( other._value );
            _error = std::move( other._error );
            _ok = other._ok;
            other._ok = false;
            return (*this);
        }
        inline Myll::Result<T, E>& operator=(const Myll::Result<T, E>& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            _value = other._value;
            _error = other._error;
            _ok = other._ok;
            return (*this);
        }
        inline bool isOk() const
        {
            return _ok;
        }
        inline bool isFail() const
        {
            return !_ok;
        }
        inline T& value()
        {
            return _value;
        }
        inline E& error()
        {
            return _error;
        }
        inline T valueOr(T fallback) const
        {
            return _ok ? _value : fallback;
        }
        inline E errorOr(E fallback) const
        {
            return _ok ? fallback : _error;
        }
    };
}
