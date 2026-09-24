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
namespace LocalNS {}
class Container;
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
int check_module();
int check_class();
