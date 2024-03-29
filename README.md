![image](https://user-images.githubusercontent.com/634372/146655353-6066d0ac-cf61-4445-abd3-0b3d0e311900.png)

# Myll
IPA: /mʏl/, free from »MyLang«. A saner Programming Language that compiles to **C++**.

Created by Jan Reitz. Licence undecided (apparently its open source).

## Goals of this Programming Language
* Provide newcomers an easy onboarding and fewer bad surprises.
* Provide professionals the comfort of known semantics with less repetition and fewer accidents.

## Principles
1. Don't ask the user to repeat themselves
2. Exceptional behavior needs to be explicit
3. What you expect to happen most cases can be implicit
4. Don't break with C++'s general semantic
5. Do break with C if there is a benefit
6. Evolve the syntax that it can't be ambiguous
7. Be useful even if a one-time-to-C++ translation is all that's needed
8. Don't be greedy with new keywords when readability benefits

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
    - DONE: [only-raw] pointer/array, output
    - DONE: prototypes for simple gen
    - subclasses
    - test cases for thesis: the_final_test.myll
    - generate functions above the variables
    - gen proper globals:
        - either:
            - extern int a;             // in .h
            - int a = 9;                // in one single .cpp
            - extern const int b;       // in .h
            - extern const int b = 9;   // in other .cpps
        - or:
            - inline int a = 9;         // in .h
            - inline const int b = 9;   // in .h

- Prio 2:
    - Benchmark the compiler
    - accessors, 100% concept, identify use cases and morph them

- Prio 3:
    - DONE: intelligent extra linespacing (between output groups)
    - automatic includes (e.g. recognize std::vector)

- fix:
    - ppp at end of struct
    - import statements which contain pathes

- done:
    - [done] common statements, out
    - [done] common expressions, out
    - [done] module
    - [done] casting, out
    - [done] multiple files
    - [done?] split var/field/global, yes!
    - [done?] static, out
    - [done?] make attributes work
    - [done?] inline, out
    - [done?] ppp in namespaces
    - [partly] manual includes (OR just do the common includes!)

- Don't forget high level ideas!
    - SoA/AoS/AoSoA attribute
    - PIMPL attribute
    - Source file unification build
    - Glue classes (e.g. you include both; iostream and vector and **only then** get an stream.operator<<(vector). Neither iostream would force you to include vector, not vector would enforce iostream)
    - Memoization as language feature
    - Static init of singletons, optimizing away the thread lock
    - *typename* instead of *var*
    - cost of moving, cost of comp, constexpr to switch algs, see "speed is in the minds of ppl"
    - less shadow language -> no preprocessor
        - solve by having compile time descisions handled in the language itself, via attribute sections
    - default arguments possible from callsite (like LINE and FILE)

# Modules
C++'s modules already foreshadow and Myll already supports the idea, although a bit different in their current form.
Myll modules specify in which .cpp/.h file things end up, when you specify "module test;" in 2 .myll files they will merge their output to a test.h and test.cpp file.
If you don't specify a module, the original filename is used as its module, test.myll is implicily "module test;".

# Wanted deviation (_breakage_) from C++
There are plenty of differences to C++, but only the ones listed here could somehow be considered downsides to C++.
- No uncontained using namespace. It would introduce the leaking C++ suffers from.
- Headers which are included need to be order-independant and must not need preceeding #defines.

# Schreiben
Äpfel mit Birnen mischen... und was sich sonst noch alles an Früchten findet.
static hat etliche Bedeutungen, const auch:
...Echte 'Konstanten' wurden aber sehr lange nicht etwa mit 'const float PI = 3.14f;'
geschrieben, sondern mit '#define PI 3.14f'

# Unsolved Issues
(not to be mistakened with not-implemented features)
- Non-type template parameters need a distinction.
- Comments do not get passed through
- Init of static data members, esp. with templates
