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
#include <vector>
namespace Outer::Inner {}
namespace LocalNS {}
class Container;
namespace Inner = Outer::Inner;
namespace LocalNS
{
    int localValue();
}
class Container
{
public:
    std::vector<int> values{};
    Container();
    int sum();
};
namespace Outer
{
    namespace Inner
    {
        int ping();
    }
}
int check_module();
int check_class();
int run_ns_alias_checks();
