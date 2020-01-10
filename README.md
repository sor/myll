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
