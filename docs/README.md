# Myll Project Documentation

Welcome to the Myll project documentation. This folder contains living documents for the Myll programming language and its compiler implementation.

## Quick Navigation

### I want to understand the language design
→ Read [`design/01-about.md`](design/01-about.md) for the mission and principles.

### I want to know the syntax
→ Read [`design/02-myll-language.md`](design/02-myll-language.md) for the full specification.

### I want to understand how Myll relates to C++
→ Read [`design/03-cpp-relationship.md`](design/03-cpp-relationship.md) for the semantic contract.

### I want to understand a specific feature
→ Check [`design/features/`](design/features/) for high-level feature families.
→ Check [`design/solutions/`](design/solutions/) for individual design decisions.

### I want to work on the compiler
→ Read [`analysis/01-architecture.md`](analysis/01-architecture.md) for the big picture.
→ Read [`analysis/08-actionable-fixes.md`](analysis/08-actionable-fixes.md) for the prioritized task list.

### I want to know what's broken and what to fix first
→ [`analysis/08-actionable-fixes.md`](analysis/08-actionable-fixes.md) — ranked by impact.

### I want to know about dead/unused code intended for the future
→ [`analysis/07-future-stubs.md`](analysis/07-future-stubs.md) — index of planned features.

### I want to understand test coverage
→ [`analysis/06-test-coverage.md`](analysis/06-test-coverage.md).

## Directory Structure

```
docs/
├── README.md                       # This file
├── design/
│   ├── 01-about.md                 # Mission, principles, audience
│   ├── 02-myll-language.md         # Full language specification (living)
│   ├── 03-cpp-relationship.md      # C++ semantic contract & deviations
│   ├── 04-other-languages.md       # Inspiration from other languages
│   ├── features/                   # High-level feature families (Approach A)
│   │   ├── declarations.md
│   │   ├── memory-model.md
│   │   ├── control-flow.md
│   │   ├── type-system.md
│   │   ├── expressions.md
│   │   ├── error-handling.md
│   │   └── modules.md
│   └── solutions/                  # Individual design decisions (Approach B)
│       ├── smart-pointers.md
│       ├── enum-flags.md
│       ├── casts.md
│       ├── switch-statement.md
│       ├── attributes.md
│       └── modules.md
└── analysis/
    ├── 01-architecture.md          # Architecture & design quality
    ├── 02-grammar-visitors.md      # Grammar & visitor completeness
    ├── 03-ast-core.md              # AST model review & bugs
    ├── 04-generator.md             # Code generator assessment
    ├── 05-frontend.md              # Frontend/driver review
    ├── 06-test-coverage.md         # Test suite evaluation
    ├── 07-future-stubs.md          # Unused code for future use
    └── 08-actionable-fixes.md      # Prioritized fix list
```

## Status Legend

Documents are living. Features marked [planned] in `02-myll-language.md` are not yet fully implemented.

For implementation status of specific features, cross-reference with the `analysis/` files.

## Contributing

When modifying the compiler, update the relevant `design/` or `analysis/` file.
- Add new syntax → update `02-myll-language.md`.
- Fix a bug → note it in the relevant `analysis/` file and check off in `08-actionable-fixes.md`.
- Implement a new feature → document in `design/features/` or `design/solutions/`.
