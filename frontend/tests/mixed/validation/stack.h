#pragma once
#include <memory>
#include <utility>
#include <cmath>
namespace Rethmann
{
    namespace OOA
    {
        namespace Uebung2_3
        {
            class StackEmptyException;
            template <typename T>
            class Stack;
            class StackEmptyException
            {
            };
            template <typename T>
            class Stack
            {
                T *_values;
                int _size;
                int _last;
              public:
                Stack()
                {
                    _last = 0;
                    _size = 8;
                    _values = new T[_size]();
                }
                explicit Stack(int size)
                {
                    _last = 0;
                    _size = size;
                    _values = new T[_size]();
                }
                ~Stack()
                {
                    delete[] _values;
                }
              private:
                bool isFull()
                {
                    return _last == _size;
                }
                void increase()
                {
                    T *t = new T[_size * 2]();
                    for( int i = 0; i < _size; ++i )
                    {
                        t[i] = _values[i];
                    }
                    _size *= 2;
                    delete[] _values;
                    _values = t;
                }
              public:
                bool isEmpty()
                {
                    return _last == 0;
                }
                Stack& push(T value)
                {
                    if( isFull() )
                    {
                        increase();
                    }
                    _values[_last] = value;
                    _last += 1;
                    return *this;
                }
                T top()
                {
                    if( isEmpty() )
                    {
                        throw StackEmptyException();
                    }
                    return _values[_last - 1];
                }
                void pop()
                {
                    if( isEmpty() )
                    {
                        throw StackEmptyException();
                    }
                    _last -= 1;
                }
            };
        }
    }
}
