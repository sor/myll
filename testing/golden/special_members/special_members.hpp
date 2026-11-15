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
struct RuleZero;
struct RuleAnyBuffer;
struct RuleBuffer;
struct Buffer;
struct Handle;
struct DefaultCopy;
struct UBox;
struct PtrBox;
struct NamedBox;
struct DefaultBox;
template <typename T>
class NamedInitList;
template <typename T>
class DefaultInitList;
struct RuleZero
{
    int value = 0;
    RuleZero();
};
struct RuleAnyBuffer
{
    std::unique_ptr<int[]> _data{};
    std::uint64_t _size = 0;
    RuleAnyBuffer() = default;
    RuleAnyBuffer(const RuleAnyBuffer& other);
    ~RuleAnyBuffer() = default;
    RuleAnyBuffer& operator=(const RuleAnyBuffer& other);
};
struct RuleBuffer
{
    std::unique_ptr<int[]> _data{};
    std::uint64_t _size = 0;
    RuleBuffer();
    explicit RuleBuffer(std::uint64_t size);
    RuleBuffer(const RuleBuffer& other);
    ~RuleBuffer();
    RuleBuffer& operator=(const RuleBuffer& other);
};
struct Buffer
{
    std::unique_ptr<int[]> _data{};
    std::uint64_t _size = 0;
    Buffer();
    explicit Buffer(std::uint64_t size);
    Buffer(const Buffer& other);
    ~Buffer();
    Buffer& operator=(const Buffer& other);
};
struct Handle
{
    int* _ptr{};
    Handle();
    explicit Handle(int value);
    Handle(const Handle& other);
    Handle(Handle&& src);
    ~Handle();
    Handle& operator=(const Handle& other);
    Handle& operator=(Handle&& src);
};
struct DefaultCopy
{
    int value = 0;
    DefaultCopy();
    DefaultCopy(const DefaultCopy& other) = default;
    ~DefaultCopy();
    DefaultCopy& operator=(const DefaultCopy& other);
};
struct UBox
{
    std::unique_ptr<int[]> data{};
    UBox();
    explicit UBox(std::uint64_t n);
    UBox(UBox&& src);
    UBox& operator=(UBox&& src);
};
struct PtrBox
{
    std::unique_ptr<int> ptr{};
    PtrBox();
    explicit PtrBox(int value);
    PtrBox(PtrBox&& src);
};
struct NamedBox
{
    int value = 0;
    NamedBox();
    explicit NamedBox(int v);
    NamedBox(const NamedBox& other);
    NamedBox(NamedBox&& src);
    NamedBox& operator=(const NamedBox& other);
    NamedBox& operator=(NamedBox&& src);
};
struct DefaultBox
{
    int value = 0;
    DefaultBox();
    explicit DefaultBox(int v);
    DefaultBox(const DefaultBox& other);
    DefaultBox(DefaultBox&& other);
    DefaultBox& operator=(const DefaultBox& other);
    DefaultBox& operator=(DefaultBox&& other);
};
template <typename T>
class NamedInitList
{
public:
    int sum = 0;
    NamedInitList(const typename std::initializer_list<T>& list)
    {
        const T* it = list.begin();
        const T* last = list.end();
        while( it != last ) {
            sum += static_cast<int>( (*it) );
            ++it;
        }
    }
    inline NamedInitList<T>& operator=(const typename std::initializer_list<T>& list)
    {
        const T* it = list.begin();
        const T* last = list.end();
        sum = 0;
        while( it != last ) {
            sum += static_cast<int>( (*it) );
            ++it;
        }
        return (*this);
    }
};
template <typename T>
class DefaultInitList
{
public:
    int sum = 0;
    DefaultInitList(const std::initializer_list<T>& init)
    {
        const T* it = init.begin();
        const T* last = init.end();
        while( it != last ) {
            sum += static_cast<int>( (*it) );
            ++it;
        }
    }
    inline DefaultInitList<T>& operator=(const std::initializer_list<T>& other)
    {
        const T* it = other.begin();
        const T* last = other.end();
        sum = 0;
        while( it != last ) {
            sum += static_cast<int>( (*it) );
            ++it;
        }
        return (*this);
    }
};
int run_rule_of_n();
int run_special_ctors();
int run_special_member_names();
