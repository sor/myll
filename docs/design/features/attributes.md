# Attributes and Scoping

This document drafts the semantic model for Myll attributes. Syntax is covered in `docs/design/solutions/attributes.md`. The `Documentation/` folder is legacy and read-only.

## Two attribute forms

| Form | Meaning | Example |
|---|---|---|
| `[pub]:` | Sets a default for all following declarations until overridden. | `[pub]: class A { ... }` |
| `[pub]` | Applies only to the very next declaration, then reverts to the last `[access]:` default. | `[pub] class A {}` |

The second form must not leak its access or attribute changes past the single declaration or block it prefixes.

## Canonical categories

Every attribute eventually normalizes to `category=value`. Built-in attributes may have several aliases that map to the same canonical pair. Some categories use a four-level severity scale taken from the legacy design notes: `forbidden`, `discouraged`, `encouraged`, `enforced`.

| Category | Values | Non-trivial aliases | Applies to |
|---|---|---|---|
| access | @struct __pub__, @class __priv__, prot | — | declarations inside a structural |
| visibility | external, internal, inline, visible | extern/external; hide/hidden → internal; module → internal unless dynamically visible | module/namespace func / var |
| scope or binding | __instance__, shared | — | fields and methods inside a class |
| lifetime | __auto__, persist | — | local var inside functions |
| execution | __BT__ (RT, AT), CT | AT = any_time, RT = run_time, CT = compile_time, BT = best_time | func, operator, var |
| effect | pure, impure, const | — | func, operator, method |
| exception | throw, nothrow | — | func |
| inheritance | @struct __discourage__, @class __allow__, finalize | final → finalize | structural |
| dispatch | @struct __static__ == nonvirtual, @class __dynamic__ (virtual, abstract, override, final) | virtual, override, final, abstract, nonvirtual | methods and operators |
| (virtual) | see dispatch | - | structural |
| conversion | auto, implicit, explicit | — | constructor |
| unhiding | on, off | shadow → off, unshadow → on | methods and structurals |
| enum | flags, manual | — | enum |
| alignment | integer value | pack(n), align(n) | type or variable |
| rule_of | @struct __none__, @class encourage __any__ (0, 3, 5) | rule_of_n → any; rule_of_5 | structural |
| pod | @struct __encourage__, @class __allow__; if set, sets access=default and enforce dispatch=static |  | structural |

`scope` applies to class fields and methods. `lifetime` applies to local variables in functions. `visibility` applies to module/namespace-level declarations.

In this example the dtor will be virtual: `[encourage dispatch=dynamic] class A { dtor {} }`. No other methods are virtual by default.

## Replacing `static`

The C++ `static` keyword is overloaded. Myll splits it by meaning:

- class-level `static` → `scope=shared` → shortcut `shared`
- module/file-level `static` (internal linkage) → `visibility=internal` → shortcut `hidden`
- local function `static` (persistent storage) → `lifetime=persist` → shortcut `persist`

The plain `[static]` attribute is intentionally not used.

## Definitions

- `pure` — does not modify global state or arguments.
- `const` — does not modify `this` state; a weaker guarantee than `pure`.
- `effect=default` resolves per declaration kind: getters default to `pure`, ordinary functions to `impure`.
- `dispatch` values map to C++ virtual-dispatch behavior. Shortcuts like `[abstract]`, `[override]`, and `[final]` are aliases for the corresponding `dispatch` value combined with any extra requirements (`abstract` also implies no implementation).
- `execution` values map roughly to C++ as: `CT` → `consteval`, `AT` → `constexpr`, `RT` → no special keyword, `BT` → `constexpr` if possible, otherwise no special keyword.

## Method hiding (unhiding)

When a derived class redeclares a base method name with a different signature, Myll normally emits C++ `using Base::name;` so both overloads participate in overload resolution. The `[shadow]` and `[unshadow]` attributes override this behavior.

- `[shadow]` on a method prevents that method from triggering `using Base::name;`.
- `[unshadow]` on a method forces `using Base::name;` for that method.
- `[shadow]` on a `class` or `struct` applies to every method declared directly in that type.
- `[unshadow]` on a `class` or `struct` applies to every method declared directly in that type.
- Method-level attributes take precedence over type-level attributes.
- The default is `Dialect.AutoUnhideBaseMethods` (true).
- A class-level attribute affects only that class and is not inherited by later derived classes.

## Special function bodies

Myll uses attributes for C++ special-member-function bodies instead of special syntax:

```myll
class Base {
[pub]:
    [abstract]
    func value() -> int;              // C++: virtual int value() = 0;
};

class Derived : Base {
[pub]:
    [override]
    func value() -> int => 42;        // C++: int value() override;
};

class Copyable {
[pub]:
    [default]
    operator copy = other;            // C++: T& operator=(const T&) = default;

    [delete]
    func badOverload( f32 x ) -> int; // C++: int badOverload(float) = delete;
};
```

- `[abstract]` may only appear on methods inside a class or struct. Myll automatically emits `virtual` for abstract methods; adding `[virtual]` explicitly triggers a warning.
- `[default]` and `[delete]` (alias `[disallow]`) may appear on functions, operators, constructors, and destructors. They must not have a body.
- `[default]` on ordinary free functions is invalid C++ unless the function is a special member; Myll currently emits it and relies on the C++ compiler to reject it.

## Per-declaration-kind defaults

A value of `default` resolves differently depending on what kind of declaration it is applied to. For example:

- `[effect=default]` on a getter resolves to `pure`.
- `[effect=default]` on a normal function resolves to `impure`.
- `[visibility=default]` on a module variable resolves to `visible`.
- `[access=default]` on a class field resolves to `priv`; on a `struct` field it resolves to `pub`.

This replaces the idea of an attribute having a fixed default value across all declaration kinds.

## Scope rules

- A `[...]:` block sets the default state for the current declaration scope.
- Entering a new scope (class, namespace, function body, braced block) saves and restores the surrounding default state.
- A single-line `[...]` temporarily overrides the default while visiting the next declaration or braced block, then restores it.
- A `[*...]:` block marks its entries as propagated, so child scopes inherit them unless they override the value or reset it to `default`.

## Severity prefixes

Severity keywords can prefix **any** attribute. Without a prefix, the attribute simply sets a value; with a prefix, it turns the attribute into a policy that the compiler checks.

| Prefix | Meaning |
|---|---|
| `forbidden` | Error if the property is present or used. |
| `discouraged` | Warning if the property is present or used. |
| `encouraged` | Warning if the property is missing or violated. |
| `enforce` / `enforced` | Error if the property is missing or violated. |
| `must` | Alias for `enforce`. |
| `allow` | Opt out of an inherited restriction for the next declaration or block. |

### Examples

```myll
[pure] func foo();                  // set value
[enforce pure] func bar();          // error if bar cannot be pure
[discourage access=prot] class A;   // warn when protected is used in A
[forbidden virtual] class B;        // virtual is not allowed in B
[encourage effect=pure]:            // warn when a function is not pure
[must effect[op]=pure, CT]          // operators must be pure; CT is just set
```

A prefix can also restrict the allowed values of a category:

```myll
[enforce access=pub|priv]   // prot is an error in this scope
```

`allow` exempts the next declaration or block from an inherited restriction:

```myll
[forbidden virtual] class B {
    [allow virtual] func special();   // this one virtual is OK
}
```

A later restriction overrides an inherited one for the same category.

Without a severity prefix, an attribute that cannot apply to a declaration is silently skipped (or passed down to children if the declaration is a hierarchical).

## Dialects (rule sets)

A dialect is a named collection of attribute defaults, propagations, restrictions, and standard-library mappings that lives in a separate configuration file. It is expected to be referenced near the top of a module file, but the exact placement and syntax are not decided yet.

The exact syntax is not decided yet. Options being considered:

- A top-level keyword:
  ```myll
  module A;
  import B;
  dialect "sor-std-game";
  func main() -> int { ... }
  ```
- An attribute on the first line:
  ```myll
  [dialect "sor-std-game"]
  module A;
  ```

All files that declare the same module must use the same dialect. Mixing dialects inside one module — for example `QString` in one file and `std::string` in another — would be an error, because the module compiles to a single translation unit with a single set of standard-library mappings.

A dialect can configure which C++ headers/types back Myll built-in keywords:

| Keyword | Default mapping | Possible dialect override |
|---|---|---|
| `string` | `std::string` (`<string>`) | `QString`, custom string type, or disabled |
| smart pointers (`*!`, `*?`) | `std::unique_ptr` / `std::shared_ptr` (`<memory>`) | custom allocator or disabled |
| math functions | `<cmath>` | disabled |

Dialects are not implemented yet; see `REASONS.md`.

## Out of scope for now

User-defined attributes via `aspect` and template constraints via `concept` are parsed but intentionally not implemented yet. See `REASONS.md`.

## TODO: current code migration

Once the design is finalized, the implementation will need to:

1. Introduce an attribute registry / canonicalization table.
2. Replace the mutable `curAccess` field in `DeclVisitor` with a saved/restored state containing `defaultAccess` and a normalized `AttributeSet`.
3. Implement `[...]:` as default-state update and `[...]` as single-declaration override.
4. Update variable/field generation to read canonical categories (`scope`, `visibility`, `lifetime`, `execution`) instead of hard-coded checks like `IsStatic`, `IsHidden`, etc.
5. Update existing test cases to use the new names.
