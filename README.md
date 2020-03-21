# Myll
IPA: /mʏl/, free from »MyLang«. A saner Programming Language that compiles to C++.

Created by Jan Reitz. Licence undecided (apparently its open source).

## Goals
1. Don't ask the user to repeat themselves, if it's not necessary
2. Exceptional behavior needs to be explicit
3. What you naively expect to happen in >=75% cases can be implicit or default
4. Don't break with C++'s general semantic
5. Do break with C if there is a benefit
6. Evolve the syntax that it can't have a Most Vexing Parse or alike
7. Be useful even if a one time to C++ translation is all that's needed
8. Don't be greedy with new keywords, the readability benefits

## Timeline
- get lexer/parser working for the big test case
- analyze most common expressions, statements, decls
- get working output **without** finishing the symbol resolve
    - easy code must work
    - different operator precedence must generate proper code
    - function pointers, arrays (the ulgyness that I wanted to fix)
    - templates (the easy cases)
- partial symbol resolve (needed for higher level features)
- see TODO
- ???
- profit

## TODO
- Prio 1 Output:
    - pointer/array, output
    - manual includes
    - common statements, output
    - common expressions, output

- Prio 2:
    - split var/field/global, yes!
    - make attributes work
    - static, input
    - accessor, 100% concept, identify use cases and morph them
    - module grammar
    - casting, output

- Prio 3:
    - inline, in/out
    - intelligent extra linespacing (between output groups)
    - multiple files
    - automatic includes (e.g. recognize std::vector)

- fix:
    - ppp in namespaces
    - ppp at end of struct

- Don't forget high level ideas!
    - SOA attribute
    - PIMPL attribute
    - Source file unification build
    - Glue classes
    - Memorization as language feature
    - Static init of singletons, optimizing away the thread lock
    - *typename* instead of *var*
    - cost of moving, cost of comp, constexpr to switch algs, see "speed is in the minds of ppl"
    - no shadow language -> no preprocessor
        - solve by having compile time descisions handled in the language itself
    - default arguments possible from callsite (like LINE and FILE)

# Modules
C++'s modules already foreshadow and Myll already supports the idea, although a bit different in their current form.
Myll modules specify in which .cpp/.h file things end up, when you specify "module test;" in 2 .myll files they will merge their output to a test.h and test.cpp file.
If you don't specify a module, the original filename is used as its module, test.myll is implicily "module test;".

# Missing Stuff from C++
There are plenty of differences to C++, but only the ones listed here could somehow be considered downsides to C++.
- No uncontained using namespace. It would introduce the leaking C++ suffers from.
- Headers which are included need to be order-independant and must not need preceeding #defines.

# Schreiben
Äpfel mit Birnen mischen... und was sich sonst noch alles an Früchten findet.
static hat etliche Bedeutungen, const auch:
...Echte 'Konstanten' wurden aber sehr lange nicht etwa mit 'const float PI = 3.14f;'
geschrieben, sondern mit '#define PI 3.14f'

# Pointer / Array output

int a;			declare a as int

int *a;			declare a as pointer to int
int a[];		declare a as array of int

int *a[];		declare a as array of pointer to int
int *(a)[];		declare a as array of pointer to int
int (*a)[];		declare a as pointer to array of int

ptr<int> a;
ary<int> a;

ary<ptr<int>> a;
ptr<ary<int>> a;

ptr<int(*)[]> a;

