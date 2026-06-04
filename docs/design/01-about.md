# About Myll

## Identity

Myll (IPA: /mʏl/, from "My Language") is a programming language that compiles to C++. It preserves C++ semantics while providing a redesigned syntax that reduces repetition, prevents common mistakes, and improves readability.

## Dual Audience

- **Newcomers**: Easier onboarding with fewer surprises and clearer syntax.
- **Professionals**: Familiar semantics with less boilerplate and fewer accidents.

## Core Principles

1. **Don't ask the user to repeat themselves**
2. **Exceptional behavior needs to be explicit**
3. **What you expect to happen most often can be implicit**
4. **Don't break with C++'s general semantics**
5. **Do break with C if there is a benefit**
6. **Evolve the syntax so it cannot be ambiguous**
7. **Be useful even if a one-time-to-C++ translation is all that's needed**
8. **Don't be greedy with new keywords when readability benefits**

## Design Philosophy

Myll is a transpiler: it generates C++ source code rather than machine code. This enables:
- Seamless interoperability with existing C++ codebases
- Incremental adoption (translate once, keep the C++)
- Compilation through any standards-compliant C++ compiler
- A hackable toolchain: download and adapt the language to your needs

The author refused to build a toy calculator or toy language. C++ was chosen deliberately because it's the right level of complexity — a language nobody fully masters, leaving genuine room for improvement.

## Why Not Just C++?

C++ has accumulated decades of syntax inconsistencies, implicit hazards, and boilerplate requirements. Myll addresses these without abandoning C++'s core strengths: zero-cost abstractions, direct hardware control, and extensive ecosystem compatibility.

Myll should feel familiar but different — diverging from C++ in its own direction, not just following the committee's evolution. Otherwise it would be pointless.
