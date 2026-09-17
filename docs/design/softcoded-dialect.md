# Softcoded dialect configuration

## Status

Design note. No implementation yet. Tracked in `plan/TODO.md` under
"Short-term consistency fixes".

## Problem

`backend/Core/Dialect.cs` exposes every Myll language switch as a C# property on a
record, and the compiler starts from the hardcoded `Dialect.Default` instance. If a
project or a library wants a different default — e.g. `StrictNew = true`,
`GlobalUsingNS = Contained`, or a custom `StringClassName` — it currently has to edit
and rebuild Myll. That is fine for bootstrapping but not for users, and it prevents
shipping a header-only Myll library that internally uses its own dialect.

## Goal

Dialect settings must be configurable from outside the compiler, and **dialect is a
per-module concept**. A single compilation can contain many modules with different
dialects, so a library can keep its own idioms while the consuming project keeps
theirs. Missing dialect configuration for a module is perfectly legal: it simply
means "auto", which resolves to the built-in default or a folder-level default.

Sources of dialect configuration, from most specific to least specific:

1. A module attribute pointing to a JSONC file (most specific).
2. A module attribute with inline key/value pairs (convenience for one-off tweaks).
3. The nearest folder-level `.myll-dialect.jsonc` file, searched upward from the
   module's source files.
4. Per-compilation CLI flags (`--dialect KEY=VALUE`).
5. Per-compilation CLI dialect file (`--dialect-file PATH`).
6. The built-in `Dialect.Default` fallback.

Each `ModuleContext` already stores its own `Dialect` instance, so the architecture
already supports per-module dialects.

## Scope of settings that should be softcoded

All scalar, enum, and string properties of `Dialect` should be overridable:

- `StringClassName`
- Pointer templates (`UniquePtrTemplate`, `SharedPtrTemplate`, etc.) and their
  `make_*` variants
- `StrictNew`
- `Shadowing`
- `SwitchFallthrough`
- `AutoUnhideBaseMethods`
- `GlobalUsingNS`
- `BaseClassAliasName`, `OwnClassAliasName`, `AutoReturnName`
- `DefaultAttributesClass/Struct/Union/Enum`
- `FloatKeyword`
- `MixedSignedness`
- `DefaultInt`, `DefaultUInt`, `DefaultFloat`, `DefaultBint`

Derived helpers such as `GetPointerTemplate` and `DefaultFloatSize()` keep reading
from the configured properties, so no API churn is needed beyond construction.

## Configuration format: JSONC

Pure JSON is unfriendly to hand-edited configuration. We use **JSON with comments**
(JSONC): `//` line comments, `/* */` block comments, and trailing commas. `System.Text.Json`
natively supports this with `JsonCommentHandling.Skip` and `AllowTrailingCommas = true`.
Booleans, numbers, strings, and enum names follow the C# property names in camelCase
or PascalCase.

Example `.myll-dialect.jsonc`:

```jsonc
{
  // This library never wants implicit raw pointers from `new`.
  "strictNew": true,

  "globalUsingNS": "Contained",

  /*
   * Bundle an alternative string type so this module is self-describing
   * when imported into projects that do not use std::string.
   */
  "stringClassName": "std::string",

  "defaultInt": "Size32",
}
```

Inline module attribute example:

```myll
module mylib;
[dialect(strictNew = true, globalUsingNS = "Contained")]

namespace Mylib:
// ...
```

## Per-module discovery and validation rules

- A module is the unit identified by the `module NAME;` declaration, not by the
  source file name.
- If multiple source files of the same module carry a `[dialect(...)]` attribute,
  their effective dialect settings must be identical. A mismatch is a compile-time
  error. The compiler should report which files disagree and on which keys.
- If none of the module's files declare a dialect, the compiler searches upward for
  `.myll-dialect.jsonc` starting from each source file directory. The first one found
  wins; directories higher in the hierarchy are fallbacks.
- If no folder-level file is found, the module uses the CLI/global default, which
  itself falls back to `Dialect.Default`.
- The special value "auto" (or simply omitting a key) means "inherit from the next
  less-specific source". It is never an error for a module to have no explicit
  dialect.

Example project layout:

```text
project/
  .myll-dialect.jsonc          # project-wide default
  src/
    app.myll                    # module app; uses project default
    lib1/
      .myll-dialect.jsonc       # lib1 default
      lib1_a.myll               # module lib1_a; uses lib1 default
      lib1_b.myll               # module lib1_b; uses lib1 default
    lib2/
      lib2_a.myll               # module lib2_a; uses project default
      lib2_b.myll               # module lib2_b; [dialect(...)] override
```

This allows `lib1` to be a header-only Myll library that ships its own dialect
without forcing that dialect on the rest of the project.

## Proposed CLI surface

```bash
# Single flag, applied globally to modules that do not override it
--dialect strict_new=true
--dialect global_using_ns=contained
--dialect string_class_name=std::string
--dialect default_int=size32
--dialect switch_fallthrough=explicit

# Global dialect file
--dialect-file project.dialect.jsonc
```

CLI flags act as the global default, not as a forced override, so per-module and
per-folder configurations still win.

## Proposed implementation path

1. Parse dialect files with `System.Text.Json` using `ReadCommentHandling =
   JsonCommentHandling.Skip` and `AllowTrailingCommas = true`. No custom tokenizer
   is needed for JSONC.
2. Add `Dialect.ParseOverrides(string jsoncText)` or similar that maps JSON keys to
   `Dialect` properties. Unknown keys error; invalid enum or type values error.
3. Add a `ModuleContext.Dialect` resolution step that, after files are grouped by
   module, collects `[dialect(...)]` attributes and folder-level config files in the
   priority order above, validates consistency across files of the same module, and
   stores the resulting `Dialect` on the `ModuleContext`.
4. Replace remaining direct reads of `Dialect.Default` with reads from the active
   `ModuleContext.Dialect`.
5. Add regression tests:
   - A module that uses a folder-level `.myll-dialect.jsonc`.
   - A compilation with two modules that deliberately use different dialects and
     both compile.
   - A negative test where two files of the same module declare conflicting
     dialects and produce a clear error.

## Open questions

- Should a CLI `--force-dialect` switch exist to override even per-module settings?
  Useful for quick experiments, but dangerous for library code. Defer until needed.
- Should folder-level config be called `.myll-dialect.jsonc`, `myll.dialect.jsonc`, or
  something else? `.myll-dialect.jsonc` is less likely to collide with user files and
  signals "all Myll modules here".
- Should dialect settings ever affect the parser/lexer? Currently they do not; all
  keywords are fixed. Keep it that way unless a concrete need arises.
- Do we want named dialect presets (`--dialect-preset strict`, `--dialect-preset cpp23`)?
  Deferred to a follow-up.

## Relationship to other work

- `plan/contained-global-using.md` depends on softcoded dialects: libraries will want
  to select `GlobalUsingNS = Contained` without recompiling Myll.
- The `string` keyword backing (`StringClassName`) is already a dialect property; once
  softcoded, libraries can choose a different string implementation per module.
