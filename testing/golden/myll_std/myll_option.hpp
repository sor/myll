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
    template <typename T>
    class Option;
    template <typename T>
    class Option
    {
    public:
        static inline Myll::Option<T> some(const T& value)
        {
            return Option( value );
        }
        static inline Myll::Option<T> none()
        {
            return Option();
        }
    private:
        T _value{};
        bool _has = false;
    public:
        Option()
        {
        }
        explicit Option(const T& value)
        {
            _value = value;
            _has = true;
        }
        Option(const Myll::Option<T>& other)
        {
            _value = other._value;
            _has = other._has;
        }
        Option(Myll::Option<T>&& other)
        {
            _value = std::move( other._value );
            _has = other._has;
            other._has = false;
        }
        inline Myll::Option<T>& operator=(Myll::Option<T>&& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            _value = std::move( other._value );
            _has = other._has;
            other._has = false;
            return (*this);
        }
        inline Myll::Option<T>& operator=(const Myll::Option<T>& other)
        {
            if( &(*this) == &other ) {
                return (*this);
            }
            _value = other._value;
            _has = other._has;
            return (*this);
        }
        inline bool has() const
        {
            return _has;
        }
        inline T& value()
        {
            return _value;
        }
        inline T valueOr(T fallback) const
        {
            return _has ? _value : fallback;
        }
        inline Myll::Option<T>& put(const T& value)
        {
            _value = value;
            _has = true;
            return (*this);
        }
        inline Myll::Option<T>& reset()
        {
            _has = false;
            return (*this);
        }
    };
}
