# Softcoded dialect configuration

## Status

Implemented. Dialects are selected in source and resolved from JSONC files in a `dialect/` directory.

## Selecting a dialect

A module declares its dialect with a top-level statement placed after `module` and before imports:

```myll
module mylib;
dialect myll;

import std_something;
// ...
```

The statement is optional. When it is omitted the module uses the `myll` dialect, which is also the internal default.

## Dialect files

Dialects are stored as files in a `dialect/` directory. For now the compiler searches the repository-root `dialect/` directory; project-local dialect directories and explicit search paths are planned later.

- `dialect/myll.jsonc` is the default dialect. It is fully populated so users can copy it as a base.
- A custom dialect is `dialect/<name>.jsonc` and is selected with `dialect <name>;`.
- The file format is JSONC: comments (`//` and `/* */`) and trailing commas are allowed.

A minimal custom dialect could look like this:

```jsonc
{
	"strictNew": true,
	"globalUsingNS": "Contained",
	"stringClassName": "std::string",
	"defaultAttributesClass": "rule_of_n=5",
}
```

Files do not have to specify every key. Missing keys fall back to the internal default, but the loader emits one warning per dialect listing the missing keys so dialect authors can make their files exhaustive. Unknown keys also produce one warning per dialect and are ignored; this keeps old dialect files working when properties are renamed or removed.

## Validation rules

- Dialect is a per-module concept, not per-file, so all source files of a module must resolve to the same effective dialect.
- If one file of a module says `dialect myll;` and another file of the same module omits the statement, both resolve to `myll` and no error is emitted.
- If files of the same module name different dialects, the compiler reports a source-located mismatch error.

## Future work

- Inheritance between dialect files (`"basedOn": "myll"`) so a dialect can store only the differences from a base dialect.
- Project-local `dialect/` directories and an explicit compiler search path.

## Relationship to other work

- `plan/contained-global-using.md`: libraries can now select `GlobalUsingNS = Contained` without recompiling Myll by shipping their own dialect file.
- `backend/Core/Dialect.cs` still owns the property model; the loader builds `Dialect` instances from JSONC instead of relying solely on the hardcoded `Dialect.Default`.
