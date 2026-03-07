# Razor Syntax (JALXAML)

This document describes Razor-style syntax support in JALXAML.

## Supported Syntax

- Path binding: `@Path`
- Expression binding: `@(expr)`
- Inline C# code block: `@{ ... }`
- Text-node mixed template: `Hello @User.Name, Count=@Count`
- Conditional block directive: `@if(expr){<Border />}`
- Escapes:
  - `@@` => literal `@`
  - `\@` => literal `@`

Razor syntax is additive sugar. Existing markup extensions like `{Binding ...}` remain fully supported.

## Resolution Order

Name/path resolution always uses:

1. `DataContext`
2. code-behind fallback (same path/member name)

If both provide the same member, `DataContext` wins.

## Update Semantics

- Observable dependency chain (`INotifyPropertyChanged` / dependency property notifications): updates in real time.
- Non-observable CLR values: evaluated once at load/initialization time.
- No polling is introduced.

## Type Rules

- Non-string target properties allow only:
  - pure path (`@Count`)
  - pure expression (`@(Count > 0 ? 1 : 0)`)
  - single computed value with code block + one output segment (`@{ var width = Count * 25; }@width`)
- Mixed templates (for example `100@x`) on non-string targets throw parse errors with location info.

## Inline C# Examples

Code block with locals:

```xml
<TextBlock Text='@{ var label = Count > 0 ? "Positive" : "Zero"; }@label' />
```

Code block with local function:

```xml
<TextBlock Text='@{ string Describe(int value) => value > 0 ? "Positive" : "Zero"; }@(Describe(Count))' />
```

Code block with manual output:

```xml
<TextBlock>
  @{ for (var i = 0; i != Count; i++) { Write(i); } }
</TextBlock>
```

`Write(...)` and `WriteLiteral(...)` are available inside `@{ ... }` blocks for advanced output scenarios.

## Error Behavior

- Build-time expression compile failures: build error.
- Runtime dynamic parse path (`XamlReader.Parse(string)`): explicit runtime exception when expression compile/evaluation fails.

## `@if` Examples

```xml
@if(IsOnline){<Border><TextBlock Text="Online" /></Border>}
@if(!IsOnline){<Border><TextBlock Text="Offline" /></Border>}
```

Inline expression equivalent:

```xml
<TextBlock Text='@(IsOnline ? "Online" : "Offline")' />
```
