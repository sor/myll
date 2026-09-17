# Softcoded dialect configuration

## Status

Design note. No implementation yet. Tracked in `plan/TODO.md` under
"Short-term consistency fixes".

## Problem

`backend/Core/Dialect.cs` exposes every Myll language switch as a C# property on a
record, and the compiler starts from the hardcoded `Dialect.Default` instance. If a
project wants a different default — e.g. `StrictNew = true`, `GlobalUsingNS =
Contained`, or a custom `StringClassName` — it must edit and rebuild the compiler.
That is fine for bootstrapping but not for users.

## Goal

Allow dialect settings to be supplied from outside the compiler without changing
`Dialect.Default`. The expected sources are, in order of priority:

1. Per-compilation CLI flags (quick experiments).
2. A project-level config file (`myll.dialect.json`, or a `[dialect]` section in a
   future `myll.toml`/`myll.json`).
3. The built-in `Dialect.Default` fallback.

Each `ModuleContext` already stores its own `Dialect` instance, so per-module
overrides are possible later.

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

## Proposed CLI surface

```bash
# single flag per value, enum names are case-insensitive
--dialect strict_new=true
--dialect global_using_ns=contained
--dialect string_class_name=std::string
--dialect default_int=size32
--dialect switch_fallthrough=explicit

# or a config file
--dialect-file myll.dialect.json
```

A JSON config file should look like:

```json
{
  "strictNew": true,
  "globalUsingNS": "Contained",
  "stringClassName": "std::string",
  "defaultInt": "Size32"
}
```

Keys use camelCase or PascalCase matching the C# property names. Unknown keys are
an error; invalid enum values are an error.

## Proposed implementation path

1. Add `Dialect.CloneWithOverrides(IReadOnlyDictionary<string, string> overrides)`
   in `backend/Core/Dialect.cs`. It uses reflection or a hand-written switch to map
   keys to properties, validates enum values, and returns a new `Dialect` instance.
2. In `frontend/CommandLineOptions.cs`, accept `--dialect KEY=VALUE` and
   `--dialect-file PATH`.
3. In `frontend/Program.cs`, build the override dictionary and pass it into
   `CompilerPipeline.CompileModules`.
4. In `backend/CompilerPipeline.cs`, clone `Dialect.Default` with the overrides
   once per compilation (or per module, once per-module config is designed).
5. Add regression tests in `testing/` that exercise at least one boolean and one
   enum override.

## Open questions

- Should the config file be project-wide or per-source-file? Per-project is the
  minimum viable step.
- Should dialect settings be visible to the parser/lexer? Currently they are not;
  all keywords are fixed. Keep it that way until a concrete need arises.
- Do we want a `--dialect-preset` switch for named bundles (e.g. `--dialect-preset
  cpp23`, `--dialect-preset strict`)? Deferred to a follow-up.

## Relationship to other work

- `plan/contained-global-using.md` depends on softcoded dialects: users will want
  to select `GlobalUsingNS = Contained` without recompiling Myll.
- The `string` keyword backing (`StringClassName`) is now a dialect property; once
  softcoded, projects can choose a different string implementation.
