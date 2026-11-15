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
#include <cstring>
#include "myll_option.hpp"
namespace Myll
{
    struct StringView;
    class String;
    struct StringView
    {
        const char* _data{};
        std::uint64_t _len = 0;
        StringView(const char* other);
        StringView(const char* data, std::uint64_t len);
        std::uint64_t len() const;
        bool isEmpty() const;
        const char& at(std::uint64_t index) const;
        const char& operator[](std::uint64_t index) const;
        bool equals(Myll::StringView other) const;
        bool startsWith(Myll::StringView other) const;
        bool endsWith(Myll::StringView other) const;
        bool contains(Myll::StringView other) const;
        bool contains(const char* cstr) const;
    };
    class String
    {
        std::unique_ptr<char[]> _data{};
        std::uint64_t _len = 0;
        std::uint64_t _capacity = 0;
    public:
        String() = default;
        explicit String(Myll::StringView view);
        String(const char* other);
        String(const Myll::String& other);
        String(Myll::String&& other);
        ~String() = default;
    private:
        void grow();
    public:
        Myll::String& operator=(Myll::String&& other);
        Myll::String& operator=(const Myll::String& other);
        std::uint64_t len() const;
        bool isEmpty() const;
        char& at(std::uint64_t index);
        char& operator[](std::uint64_t index);
        Myll::String& reserve(std::uint64_t newCapacity);
        Myll::String& pushBack(char value);
        Myll::String& append(Myll::StringView view);
        Myll::String& append(const char* cstr);
        Myll::String& append(Myll::String other);
        Myll::String& appendRepeat(char value, std::uint64_t count);
        Myll::String& clear();
        Myll::StringView view() const;
        Myll::Option<std::uint64_t> find(char value);
        Myll::Option<std::uint64_t> find(Myll::StringView view);
        Myll::Option<std::uint64_t> find(const char* cstr);
        bool contains(char value);
        bool contains(Myll::StringView view);
        bool contains(const char* cstr);
        bool startsWith(Myll::StringView prefix);
        bool startsWith(const char* cstr);
        bool endsWith(Myll::StringView suffix);
        bool endsWith(const char* cstr);
        Myll::String substring(std::uint64_t start, std::uint64_t end);
        bool operator==(Myll::StringView other) const;
        bool operator==(const char* cstr) const;
        bool operator==(const Myll::String& other) const;
        bool operator!=(Myll::StringView other) const;
        bool operator!=(const char* cstr) const;
        bool operator!=(const Myll::String& other) const;
        bool operator<(Myll::StringView other) const;
        bool operator<(const char* cstr) const;
        bool operator<(const Myll::String& other) const;
    };
}
