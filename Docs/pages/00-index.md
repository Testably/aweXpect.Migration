---
description: Migrate the assertions of FluentAssertions and xUnit to aweXpect with the analyzers and code fixes of aweXpect.Migration.
---

import ApplyFix from '@site/src/components/ApplyFix';
import PackageInstall from '@site/src/components/PackageInstall';

# Migration

`aweXpect.Migration` is a Roslyn analyzer that migrates assertions from [FluentAssertions](https://fluentassertions.com/) and [xUnit](https://xunit.net/) to [aweXpect](/aweXpect). It reports each assertion of these libraries, and its code fix rewrites the call site to the aweXpect equivalent where it knows one.

Upgrading from an older aweXpect version instead? See the upgrade guides [from aweXpect 2.x](/aweXpect/changelog/migration-to-v3) and [from aweXpect 3.x](/aweXpect/changelog/migration-to-v4).

## Installation

Install aweXpect (the assertion library you are migrating *to*) into the test project you want to migrate:

<PackageInstall package="aweXpect" />

Then add `aweXpect.Migration` (the analyzers and code fixes) as a development dependency:

<PackageInstall package="aweXpect.Migration" developmentDependency />

`aweXpect.Migration` ships only the analyzers and code fixes, no runtime code.

It is usually convenient to add the following global usings to the project so the rewritten code compiles without further edits:

```csharp
global using System.Threading.Tasks;
global using aweXpect;
```

## How it works

After installing the package, every assertion of the source library is reported as a warning:

| Diagnostic     | Source library                               | Code fix title                                      |
|----------------|----------------------------------------------|-----------------------------------------------------|
| `aweXpectM002` | [FluentAssertions](./01-fluentassertions.md) | *Migration available: FluentAssertions assertion*   |
| `aweXpectM003` | [xUnit](./03-xunit.md)                       | *Xunit assertions should be replaced with aweXpect* |

`aweXpectM002` is raised on every `.Should()` invocation defined under the `FluentAssertions` namespace (nested `.Should()` calls inside lambda arguments are intentionally skipped, so chains like `.Should().AllSatisfy(x => x.Should().BeGreaterThan(0))` are migrated as a single unit). `aweXpectM003` is raised on every method call on `Xunit.Assert`.

The code fix is offered only where it knows a rewrite. An assertion without one keeps its warning and has to be migrated by hand (see [Manual review](#manual-review)).

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
bool isReleased = // ...

// highlight-next-line
isReleased.Should().BeTrue();
```

```csharp title="After (aweXpect)"
bool isReleased = // ...

// highlight-next-line
Expect.That(isReleased).IsTrue();
```

</ApplyFix>

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
string title = // ...

// highlight-next-line
Assert.Equal("Abbey Road", title);
```

```csharp title="After (aweXpect)"
string title = // ...

// highlight-next-line
Expect.That(title).IsEqualTo("Abbey Road");
```

</ApplyFix>

The supported constructs of each source library are listed on [FluentAssertions](./01-fluentassertions.md) and [xUnit](./03-xunit.md). A few rewrites pass or fail for different inputs than the original; these cases are described on [Behavioral differences from FluentAssertions](./02-fluentassertions-differences.md) and [Behavioral differences from xUnit](./04-xunit-differences.md). Their examples use the shorter `That(...)` and assume `global using static aweXpect.Expect;` as well.

## Recommended workflow

1. **Install the packages.** Add aweXpect and `aweXpect.Migration` to the test project (see [Installation](#installation)). Every assertion of FluentAssertions and xUnit is now reported as a warning.
2. **Apply the code fixes** for `aweXpectM002` and `aweXpectM003` in your IDE or from the command line (see below).
3. **Add the missing `await`.** Expectations in aweXpect only run when they are awaited. The `aweXpect0001` analyzer that ships with `aweXpect` itself reports the rewritten expressions, and its code fix adds the `await` and marks the surrounding methods `async`.
4. **Address the call sites that need [manual review](#manual-review).** They keep their warning; migrate each one by hand. When a migrated test fails, check the behavioral differences from [FluentAssertions](./02-fluentassertions-differences.md) and [xUnit](./04-xunit-differences.md).
5. **Remove the migration package** (see [Removing the package](#removing-the-package)).

### In the IDE

Apply "Fix all in solution" for `aweXpectM002` and `aweXpectM003` from your IDE (Visual Studio, Rider, VS Code with C# Dev Kit), then "Fix all" for `aweXpect0001` to add the missing `await` and mark the surrounding methods `async`.

### From the command line

Migration is a two-pass operation: first apply the rewrites, then add the missing `await` and `async` keywords:

```shell
# 1. Rewrite assertions
dotnet format analyzers --diagnostics aweXpectM002 aweXpectM003 --severity warn

# 2. Add the missing `await` (and mark surrounding methods `async`)
dotnet format analyzers --diagnostics aweXpect0001 --severity warn
```

## Manual review

The code fix offers no rewrite when it cannot translate an assertion without losing an argument or changing its meaning. These call sites keep their warning; migrate each one by hand:

| Source library   | Construct                                                                                                     | Details                                                                             |
|------------------|---------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|
| FluentAssertions | A chain that continues on a second `.Should()`, for example after `.Which`                                    | [FluentAssertions](./01-fluentassertions.md)                                        |
| FluentAssertions | A chained call without an aweXpect counterpart, such as `Where(...)`, and any call after `WithInnerException` | [Exceptions](./01-fluentassertions.md#exceptions)                                   |
| FluentAssertions | `BeCloseTo` or `NotBeCloseTo` with a constant delta that does not fit the subject type                        | [Equality](./01-fluentassertions.md#equality)                                       |
| FluentAssertions | Equivalency options other than the ordering and, for strings and items compared by value, the string options  | [Equivalency](./01-fluentassertions.md#equivalency)                                 |
| FluentAssertions | `BeAssignableTo` or `NotBeAssignableTo` on a `System.Type` subject                                            | [Assertions on a `Type`](./02-fluentassertions-differences.md#assertions-on-a-type) |
| xUnit            | An additional argument such as a comparer or `ignoreCase: true`                                               | [Equality](./03-xunit.md#equality)                                                  |
| xUnit            | A `StringComparison` other than a constant `Ordinal` or `OrdinalIgnoreCase`                                   | [Strings](./03-xunit.md#strings)                                                    |
| xUnit            | An `exactMatch` argument that is not a `true` or `false` literal                                              | [Types](./03-xunit.md#types)                                                        |

Any other assertion that is not listed on the page of its source library keeps its warning as well.

## Suppressing the diagnostic

If you choose not to migrate a particular assertion, suppress the diagnostic per call site with the standard mechanisms:

```csharp
bool isReleased = // ...

#pragma warning disable aweXpectM002
isReleased.Should().BeTrue();
#pragma warning restore aweXpectM002
```

or via an `.editorconfig` entry scoped to a file or folder:

```text title=".editorconfig"
[**/Legacy/**.cs]
dotnet_diagnostic.aweXpectM002.severity = none
dotnet_diagnostic.aweXpectM003.severity = none
```

## Removing the package

Once the warnings in a project have been resolved, drop the analyzer reference, and FluentAssertions if no test uses it any longer:

```shell
dotnet remove package aweXpect.Migration
dotnet remove package FluentAssertions
```

`aweXpect` itself stays in the project; that is the assertion library the tests now depend on. xUnit stays as well when it runs the tests.
