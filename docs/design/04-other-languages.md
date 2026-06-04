# Inspiration from Other Languages

Myll's design is primarily a response to C++, but individual features are informed by other languages. This document records those influences where they explain design choices.

## TypeScript

**Key Lesson:** Transpilation can succeed. TypeScript proved that a stricter, cleaner syntax can compile to a messy but ubiquitous target (JavaScript) and achieve mass adoption. Myll applies the same philosophy to C++.

**Specific Influence:**
- The idea of `.d.ts` declaration files for external code signature discovery.
- Module system design for file merging.

## D

**Key Lesson:** A systems language can be cleaner than C++ without sacrificing performance. D's `import` vs `#include` model influenced Myll's module system.

**Specific Influence:**
- `import` as a true module system rather than textual inclusion.
- Attribute syntax exploration.

## Rust

**Key Lesson:** Explicit ownership and borrowing prevent entire categories of bugs. While Myll preserves C++'s memory model (including raw pointers), Rust's influence is visible in the design philosophy.

**Specific Influence:**
- Smart pointer suffixes (`!`, `+`, `?`) as lightweight ownership annotations.
- The idea that dangerous operations should look dangerous or be explicit.
- `mut` vs. immutable-by-default was considered but rejected to preserve C++ familiarity.

## C#

**Key Lesson:** Modern syntax features (properties, events, attributes) integrate well with C-family languages.

**Specific Influence:**
- Attribute syntax in square brackets.
- Property getter/setter syntax (parsed but not yet generated).
- Eventual support for events/delegates is aspirational.

## Ruby

**Key Lesson:** Programmer happiness matters. Optional parentheses and readable syntax reduce cognitive load.

**Specific Influence:**
- Minimal punctuation where unambiguous.
- `do ... end` / `do ... times` block syntax.

## Go

**Key Lesson:** Simplicity through orthogonality. Go's minimal feature set is intentionally not Myll's goal, but its approach to clear, uniform syntax is respected.
