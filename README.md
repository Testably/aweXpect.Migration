# aweXpect.Migration

[![Nuget](https://img.shields.io/nuget/v/aweXpect.Migration)](https://www.nuget.org/packages/aweXpect.Migration)
[![Build](https://github.com/Testably/aweXpect.Migration/actions/workflows/build.yml/badge.svg)](https://github.com/Testably/aweXpect.Migration/actions/workflows/build.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Testably_aweXpect.Migration&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Testably_aweXpect.Migration)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Testably_aweXpect.Migration&metric=coverage)](https://sonarcloud.io/summary/overall?id=Testably_aweXpect.Migration)

A Roslyn analyzer and code-fix provider that migrates [FluentAssertions](https://fluentassertions.com/)
and [xUnit](https://xunit.net/) assertions to [aweXpect](https://github.com/aweXpect/aweXpect). Drop the
package into a project that uses one of those libraries and the analyzer flags each assertion it can
migrate; the accompanying code fix rewrites the call site to its aweXpect equivalent.

## Installation

Install [aweXpect](https://www.nuget.org/packages/aweXpect) (the assertion library you are migrating
*to*) and `aweXpect.Migration` (the analyzers and code fixers) into the test project you want to
migrate:

```shell
dotnet add package aweXpect
dotnet add package aweXpect.Migration
```

`aweXpect.Migration` ships only the analyzer and code fixer — no runtime code. Once a project no
longer references the source assertion library you can remove the `aweXpect.Migration` reference
again; `aweXpect` itself stays.

It is usually convenient to add the following global usings to the project so the rewritten code
compiles without further edits:

```csharp
global using System.Threading.Tasks;
global using aweXpect;
```

## How it works

After installing the package, every supported assertion is reported as a warning. Apply the relevant
code fix from your IDE (Visual Studio, Rider, VS Code with C# Dev Kit) or via `dotnet format
analyzers` to rewrite the call site.

| Diagnostic     | Source library    | Code fix title                          |
|----------------|-------------------|-----------------------------------------|
| `aweXpectM002` | FluentAssertions  | *Migrate FluentAssertions to aweXpect*  |
| `aweXpectM003` | xUnit             | *Migrate xUnit assertion to aweXpect*   |

`aweXpectM002` is raised on every `.Should()` invocation defined under the `FluentAssertions`
namespace (nested `.Should()` inside lambda arguments are intentionally skipped, so chains like
`.Should().AllSatisfy(x => x.Should().BeGreaterThan(0))` are migrated as a single unit).
`aweXpectM003` is raised on every method call on `Xunit.Assert`.

Conditional access on the subject is supported — `subject?.Inner?.NewInner()?.Value.Should().Be(x)`
rewrites to `Expect.That(subject?.Inner?.NewInner()?.Value).IsEqualTo(x)`, preserving the
null-propagation chain.

A typical migration looks like this:

```csharp
// Before
subject.Should().BeTrue();
Assert.Equal(expected, actual);

// After
await Expect.That(subject).IsTrue();
await Expect.That(actual).IsEqualTo(expected);
```

Most rewritten expressions return a `Task` and must be awaited — that's enforced by the
`aweXpect0001` analyzer that ships with `aweXpect` itself.

### From the command line

Migration is a two-pass operation: first apply the rewrites, then add the missing `await` /
`async` keywords:

```shell
# 1. Rewrite assertions
dotnet format analyzers --diagnostics aweXpectM002 aweXpectM003 --severity warn

# 2. Add the missing `await` (and mark surrounding methods `async`)
dotnet format analyzers --diagnostics aweXpect0001 --severity warn
```

In an IDE the equivalent is "Fix all in solution" for `aweXpectM002` / `aweXpectM003`, then
"Fix all" for `aweXpect0001`.

## Migrating from FluentAssertions

The fixer rewrites the entire `.Should()` chain in one step: the `Should()` call is dropped,
`Expect.That(subject)` becomes the new root, the assertion methods are renamed, and any trailing
`because` argument is moved into a `.Because(...)` suffix. `.And` chains are preserved.

```csharp
// Before (FluentAssertions)
subject.Should().Contain(1).And.Contain(2, "because foo");

// After (aweXpect)
await Expect.That(subject).Contains(1).And.Contains(2).Because("because foo");
```

> The tables below are grouped by **target concept** (equality, containment, …), not by
> FluentAssertions' own documentation pages. Most of the "basic" assertions (`Be`, `NotBe`,
> `BeEmpty`, `Contain`, `StartWith`, …) are not string-specific — they apply to any compatible
> subject type.

### Equality

| FluentAssertions construct                                    | Rewritten to                                                |
|---------------------------------------------------------------|-------------------------------------------------------------|
| `subject.Should().Be(expected)`                               | `Expect.That(subject).IsEqualTo(expected)`                  |
| `subject.Should().NotBe(unexpected)`                          | `Expect.That(subject).IsNotEqualTo(unexpected)`             |
| `subject.Should().BeApproximately(expected, tolerance)`       | `Expect.That(subject).IsEqualTo(expected).Within(tolerance)`|
| `subject.Should().NotBeApproximately(expected, tolerance)`    | `Expect.That(subject).IsNotEqualTo(expected).Within(tolerance)` |
| `subject.Should().BeCloseTo(expected, delta)`                 | `Expect.That(subject).IsEqualTo(expected).Within(delta)`    |
| `subject.Should().NotBeCloseTo(expected, delta)`              | `Expect.That(subject).IsNotEqualTo(expected).Within(delta)` |
| `subject.Should().BeOneOf(a, b, c)`                           | `Expect.That(subject).IsOneOf(a, b, c)`                     |
| `subject.Should().BeOneOf(collection)`                        | `Expect.That(subject).IsOneOf(collection)`                  |
| `subject.Should().BeOneOf(value, collection)`                 | `Expect.That(subject).IsOneOf(value)`                       |
| `subject.Should().BeSameAs(expected)`                         | `Expect.That(subject).IsSameAs(expected)`                   |
| `subject.Should().NotBeSameAs(unexpected)`                    | `Expect.That(subject).IsNotSameAs(unexpected)`              |

`Should().BeEquivalentTo(...)` and `Should().NotBeEquivalentTo(...)` dispatch on the static type of
the subject:

| Subject type             | `BeEquivalentTo(expected)` rewrites to                                                    |
|--------------------------|-------------------------------------------------------------------------------------------|
| `string`                 | `IsEqualTo(expected).IgnoringCase()` (plus options below)                                 |
| `IEnumerable<T>`         | `IsEqualTo(expected).InAnyOrder()` (or `IsEqualTo(expected)` with `WithStrictOrdering()`) |
| any other reference type | `IsEquivalentTo(expected)`                                                                |

For string subjects the `o => o.…` options are translated as well (combined with the implicit
`.IgnoringCase()` above):

| FluentAssertions option                  | Appended to the rewrite                |
|------------------------------------------|----------------------------------------|
| `o => o.IgnoringLeadingWhitespace()`     | `.IgnoringLeadingWhiteSpace()`         |
| `o => o.IgnoringTrailingWhitespace()`    | `.IgnoringTrailingWhiteSpace()`        |
| `o => o.IgnoringNewlineStyle()`          | `.IgnoringNewlineStyle()`              |

`NotBeEquivalentTo` is translated symmetrically (`IsNotEqualTo` / `IsNotEquivalentTo`).

### Null and emptiness

| FluentAssertions construct                  | Rewritten to                                  |
|---------------------------------------------|-----------------------------------------------|
| `subject.Should().BeNull()`                 | `Expect.That(subject).IsNull()`               |
| `subject.Should().NotBeNull()`              | `Expect.That(subject).IsNotNull()`            |
| `subject.Should().BeEmpty()`                | `Expect.That(subject).IsEmpty()`              |
| `subject.Should().NotBeEmpty()`             | `Expect.That(subject).IsNotEmpty()`           |
| `subject.Should().BeNullOrEmpty()`          | `Expect.That(subject).IsNullOrEmpty()`        |
| `subject.Should().NotBeNullOrEmpty()`       | `Expect.That(subject).IsNotNullOrEmpty()`     |
| `subject.Should().BeNullOrWhiteSpace()`     | `Expect.That(subject).IsNullOrWhiteSpace()`   |
| `subject.Should().NotBeNullOrWhiteSpace()`  | `Expect.That(subject).IsNotNullOrWhiteSpace()`|

### Booleans

| FluentAssertions construct           | Rewritten to                          |
|--------------------------------------|---------------------------------------|
| `subject.Should().BeTrue()`          | `Expect.That(subject).IsTrue()`       |
| `subject.Should().BeFalse()`         | `Expect.That(subject).IsFalse()`      |
| `subject.Should().NotBeTrue()`       | `Expect.That(subject).IsNotTrue()`    |
| `subject.Should().NotBeFalse()`      | `Expect.That(subject).IsNotFalse()`   |
| `subject.Should().Imply(other)`      | `Expect.That(subject).Implies(other)` |

### Type checks

The generic forms (`<T>`) and the non-generic `(typeof(T))` overloads are both supported — the
fixer keeps whichever form was used.

| FluentAssertions construct                      | Rewritten to                                   |
|-------------------------------------------------|------------------------------------------------|
| `subject.Should().BeAssignableTo<T>()`          | `Expect.That(subject).Is<T>()`                 |
| `subject.Should().BeAssignableTo(typeof(T))`    | `Expect.That(subject).Is(typeof(T))`           |
| `subject.Should().NotBeAssignableTo<T>()`       | `Expect.That(subject).IsNot<T>()`              |
| `subject.Should().NotBeAssignableTo(typeof(T))` | `Expect.That(subject).IsNot(typeof(T))`        |
| `subject.Should().BeOfType<T>()`                | `Expect.That(subject).IsExactly<T>()`          |
| `subject.Should().BeOfType(typeof(T))`          | `Expect.That(subject).IsExactly(typeof(T))`    |
| `subject.Should().NotBeOfType<T>()`             | `Expect.That(subject).IsNotExactly<T>()`       |
| `subject.Should().NotBeOfType(typeof(T))`       | `Expect.That(subject).IsNotExactly(typeof(T))` |

### Numbers

| FluentAssertions construct                                      | Rewritten to                                            |
|-----------------------------------------------------------------|---------------------------------------------------------|
| `subject.Should().BePositive()`                                 | `Expect.That(subject).IsPositive()`                     |
| `subject.Should().BeNegative()`                                 | `Expect.That(subject).IsNegative()`                     |
| `subject.Should().BeGreaterThan(expected)`                      | `Expect.That(subject).IsGreaterThan(expected)`          |
| `subject.Should().BeGreaterThanOrEqualTo(expected)`             | `Expect.That(subject).IsGreaterThanOrEqualTo(expected)` |
| `subject.Should().BeGreaterOrEqualTo(expected)` *(legacy FA 7)* | `Expect.That(subject).IsGreaterThanOrEqualTo(expected)` |
| `subject.Should().BeLessThan(expected)`                         | `Expect.That(subject).IsLessThan(expected)`             |
| `subject.Should().BeLessThanOrEqualTo(expected)`                | `Expect.That(subject).IsLessThanOrEqualTo(expected)`    |
| `subject.Should().BeLessOrEqualTo(expected)` *(legacy FA 7)*    | `Expect.That(subject).IsLessThanOrEqualTo(expected)`    |
| `subject.Should().BeInRange(low, high)`                         | `Expect.That(subject).IsBetween(low).And(high)`         |
| `subject.Should().NotBeInRange(low, high)`                      | `Expect.That(subject).IsNotBetween(low).And(high)`      |

### Containment, ordering and predicates

These cover both string and collection subjects; the fixer dispatches on the parameter types where
it matters.

| FluentAssertions construct                                              | Rewritten to                                                                         |
|-------------------------------------------------------------------------|--------------------------------------------------------------------------------------|
| `subject.Should().Contain(expected)`                                    | `Expect.That(subject).Contains(expected)`                                            |
| `subject.Should().Contain(item).And.Contain(other)`                     | `Expect.That(subject).Contains(item).And.Contains(other)`                            |
| `subject.Should().Contain(collection)`                                  | `Expect.That(subject).Contains(collection).InAnyOrder().IgnoringInterspersedItems()` |
| `subject.Should().NotContain(unexpected)`                               | `Expect.That(subject).DoesNotContain(unexpected)`                                    |
| `subject.Should().StartWith(expected)`                                  | `Expect.That(subject).StartsWith(expected)`                                          |
| `subject.Should().NotStartWith(unexpected)`                             | `Expect.That(subject).DoesNotStartWith(unexpected)`                                  |
| `subject.Should().EndWith(expected)`                                    | `Expect.That(subject).EndsWith(expected)`                                            |
| `subject.Should().NotEndWith(unexpected)`                               | `Expect.That(subject).DoesNotEndWith(unexpected)`                                    |
| `subject.Should().ContainInOrder(collection)`                           | `Expect.That(subject).Contains(collection).IgnoringInterspersedItems()`              |
| `subject.Should().ContainInOrder(a, b, c)`                              | `Expect.That(subject).Contains([a, b, c]).IgnoringInterspersedItems()`               |
| `subject.Should().ContainInConsecutiveOrder(collection)`                | `Expect.That(subject).Contains(collection)`                                          |
| `subject.Should().ContainInConsecutiveOrder(a, b, c)`                   | `Expect.That(subject).Contains([a, b, c])`                                           |
| `subject.Should().ContainEquivalentOf(expected)`                        | `Expect.That(subject).Contains(expected).Equivalent()`                               |
| `subject.Should().NotContainEquivalentOf(unexpected)`                   | `Expect.That(subject).DoesNotContain(unexpected).Equivalent()`                       |
| `subject.Should().BeSubsetOf(expected)`                                 | `Expect.That(subject).IsContainedIn(expected).InAnyOrder()`                          |
| `subject.Should().NotBeSubsetOf(expected)`                              | `Expect.That(subject).IsNotContainedIn(expected).InAnyOrder()`                       |
| `subject.Should().HaveCount(n)`                                         | `Expect.That(subject).HasCount(n)`                                                   |
| `subject.Should().ContainSingle()`                                      | `Expect.That(subject).HasSingle()`                                                   |
| `subject.Should().ContainSingle(predicate)`                             | `Expect.That(subject).HasSingle().Matching(predicate)`                               |
| `subject.Should().OnlyContain(predicate)`                               | `Expect.That(subject).All().Satisfy(predicate)`                                      |
| `subject.Should().OnlyHaveUniqueItems()`                                | `Expect.That(subject).AreAllUnique()`                                                |
| `subject.Should().BeInAscendingOrder()`                                 | `Expect.That(subject).IsInAscendingOrder()`                                          |
| `subject.Should().BeInAscendingOrder(comparer)`                         | `Expect.That(subject).IsInAscendingOrder().Using(comparer)`                          |
| `subject.Should().BeInAscendingOrder(x => x.Key)`                       | `Expect.That(subject).IsInAscendingOrder(x => x.Key)`                                |
| `subject.Should().BeInDescendingOrder()`                                | `Expect.That(subject).IsInDescendingOrder()`                                         |
| `subject.Should().NotBeInAscendingOrder()` / `NotBeInDescendingOrder()` | `Expect.That(subject).IsNotInAscendingOrder()` / `IsNotInDescendingOrder()`          |
| `subject.Should().Match(predicate)` *(non-string subject)*              | `Expect.That(subject).Satisfies(predicate)`                                          |
| `subject.Should().Match(pattern)` *(string subject)*                    | `Expect.That(subject).IsEqualTo(pattern).AsWildcard()`                               |

#### Occurrence constraints on `Contain` (string subjects)

`Should().Contain(expected, occurrence)` translates the full FluentAssertions occurrence family.
Note the `Thrice` / `Times(n)` cases — they're emitted as `3.Times()` / numeric arguments, not as
chained `.Thrice()`:

| FluentAssertions occurrence | Appended to the rewrite |
|-----------------------------|-------------------------|
| `AtLeast.Once()`            | `.AtLeast().Once()`     |
| `AtLeast.Twice()`           | `.AtLeast().Twice()`    |
| `AtLeast.Thrice()`          | `.AtLeast(3.Times())`   |
| `AtLeast.Times(n)`          | `.AtLeast(n)`           |
| `AtMost.Once()`             | `.AtMost().Once()`      |
| `AtMost.Twice()`            | `.AtMost().Twice()`     |
| `AtMost.Thrice()`           | `.AtMost(3.Times())`    |
| `AtMost.Times(n)`           | `.AtMost(n)`            |
| `Exactly.Once()`            | `.Once()`               |
| `Exactly.Twice()`           | `.Twice()`              |
| `Exactly.Thrice()`          | `.Exactly(3.Times())`   |
| `Exactly.Times(n)`          | `.Exactly(n)`           |
| `LessThan.Twice()`          | `.LessThan().Twice()`   |
| `LessThan.Thrice()`         | `.LessThan(3.Times())`  |
| `LessThan.Times(n)`         | `.LessThan(n)`          |
| `MoreThan.Once()`           | `.MoreThan().Once()`    |
| `MoreThan.Twice()`          | `.MoreThan().Twice()`   |
| `MoreThan.Thrice()`         | `.MoreThan(3.Times())`  |
| `MoreThan.Times(n)`         | `.MoreThan(n)`          |

### Collections

In addition to the universal containment and ordering assertions above, these collection-specific
forms are recognised:

| FluentAssertions construct                                               | Rewritten to                                                                 |
|--------------------------------------------------------------------------|------------------------------------------------------------------------------|
| `subject.Should().AllBeAssignableTo<T>()` / `(typeof(T))`                | `Expect.That(subject).All().Are<T>()` / `All().Are(typeof(T))`               |
| `subject.Should().AllBeOfType<T>()` / `(typeof(T))`                      | `Expect.That(subject).All().AreExactly<T>()` / `All().AreExactly(typeof(T))` |
| `subject.Should().AllBeEquivalentTo(expected)` *(non-string elements)*   | `Expect.That(subject).All().AreEquivalentTo(expected)`                       |
| `subject.Should().AllBeEquivalentTo(expected)` *(`IEnumerable<string>`)* | `Expect.That(subject).All().AreEqualTo(expected)`                            |

For `IEnumerable<string>` collections, the `o => o.…` options on `AllBeEquivalentTo` are translated
as well:

| FluentAssertions option               | Appended to the rewrite        |
|---------------------------------------|--------------------------------|
| `o => o.IgnoringCase()`               | `.IgnoringCase()`              |
| `o => o.IgnoringLeadingWhitespace()`  | `.IgnoringLeadingWhiteSpace()` |
| `o => o.IgnoringTrailingWhitespace()` | `.IgnoringTrailingWhiteSpace()`|
| `o => o.IgnoringNewlineStyle()`       | `.IgnoringNewlineStyle()`      |
| `o => o.WithoutStrictOrdering()`      | `.InAnyOrder()`                |

### Object equivalency

| FluentAssertions construct                                            | Rewritten to                                         |
|-----------------------------------------------------------------------|------------------------------------------------------|
| `subject.Should().BeEquivalentTo(expected)` *(arbitrary object)*      | `Expect.That(subject).IsEquivalentTo(expected)`      |
| `subject.Should().NotBeEquivalentTo(unexpected)` *(arbitrary object)* | `Expect.That(subject).IsNotEquivalentTo(unexpected)` |

### Exceptions

| FluentAssertions construct                                       | Rewritten to                               |
|------------------------------------------------------------------|--------------------------------------------|
| `callback.Should().NotThrow()` / `NotThrowAsync()`               | `Expect.That(callback).DoesNotThrow()`     |
| `callback.Should().NotThrow<T>()` / `NotThrowAsync<T>()`         | `Expect.That(callback).DoesNotThrow<T>()`  |
| `callback.Should().Throw<T>()` / `ThrowAsync<T>()`               | `Expect.That(callback).Throws<T>()`        |
| `callback.Should().ThrowExactly<T>()` / `ThrowExactlyAsync<T>()` | `Expect.That(callback).ThrowsExactly<T>()` |
| `…WithMessage(pattern)`                                          | `…WithMessage(pattern).AsWildcard()`       |

```csharp
// Before
callback.Should().Throw<ArgumentException>().WithMessage("foo*");

// After
await Expect.That(callback).Throws<ArgumentException>().WithMessage("foo*").AsWildcard();
```

### Dates and times

| FluentAssertions construct                                                            | Rewritten to                                                                |
|---------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| `subject.Should().BeAfter(expected)`                                                  | `Expect.That(subject).IsAfter(expected)`                                    |
| `subject.Should().BeOnOrAfter(expected)`                                              | `Expect.That(subject).IsOnOrAfter(expected)`                                |
| `subject.Should().BeBefore(expected)`                                                 | `Expect.That(subject).IsBefore(expected)`                                   |
| `subject.Should().BeOnOrBefore(expected)`                                             | `Expect.That(subject).IsOnOrBefore(expected)`                               |
| `subject.Should().NotBeAfter(unexpected)` / `NotBeOnOrAfter(...)`                     | `Expect.That(subject).IsNotAfter(unexpected)` / `IsNotOnOrAfter(...)`       |
| `subject.Should().NotBeBefore(unexpected)` / `NotBeOnOrBefore(...)`                   | `Expect.That(subject).IsNotBefore(unexpected)` / `IsNotOnOrBefore(...)`     |
| `subject.Should().HaveYear(n)` (also `Month`/`Day`/`Hour`/`Minute`/`Second`/`Offset`) | `Expect.That(subject).HasYear().EqualTo(n)` (etc.)                          |
| `subject.Should().NotHaveYear(n)` (and the other parts)                               | `Expect.That(subject).HasYear().NotEqualTo(n)` (etc.)                       |

### Enums and nullable values

| FluentAssertions construct                     | Rewritten to                                  |
|------------------------------------------------|-----------------------------------------------|
| `subject.Should().BeDefined()`                 | `Expect.That(subject).IsDefined()`            |
| `subject.Should().NotBeDefined()`              | `Expect.That(subject).IsNotDefined()`         |
| `subject.Should().HaveFlag(flag)`              | `Expect.That(subject).HasFlag(flag)`          |
| `subject.Should().NotHaveFlag(flag)`           | `Expect.That(subject).DoesNotHaveFlag(flag)`  |
| `subject.Should().HaveValue()` (`Nullable<T>`) | `Expect.That(subject).IsNotNull()`            |
| `subject.Should().HaveValue(v)`                | `Expect.That(subject).HasValue(v)`            |
| `subject.Should().NotHaveValue()`              | `Expect.That(subject).IsNull()`               |
| `subject.Should().NotHaveValue(v)`             | `Expect.That(subject).DoesNotHaveValue(v)`    |

> **Caveat — `HaveValue` / `NotHaveValue` argument heuristic.** The fixer inspects the *text* of
> the first argument: anything containing a `"` (i.e. anything that looks like a string literal) is
> treated as a `because` message rather than an expected value. As a result,
> `subject.Should().HaveValue("text")` rewrites to `Expect.That(subject).IsNotNull()` instead of
> `HasValue("text")`. Review the rewrite when the expected value is itself a string.

### Lambda assertions

When the entire `.Should()` chain is the body of a lambda (so it is not awaited at the call site
and the surrounding signature is `Action` / `Func<T>`), the fixer wraps the rewritten expression in
`aweXpect.Synchronous.Synchronously.Verify(...)` to keep the lambda synchronous:

```csharp
// Before
Action action = () => true.Should().BeTrue();

// After
Action action = () => aweXpect.Synchronous.Synchronously.Verify(Expect.That(true).IsTrue());
```

### Because messages

A trailing `because` argument on any of the assertions above is preserved as a `.Because(...)`
suffix, including formatted overloads:

```csharp
// Before
subject.Should().Be(expected, "because the value is {0}", value);

// After
await Expect.That(subject).IsEqualTo(expected).Because($"because the value is {value}");
```

## Migrating from xUnit

The fixer rewrites each `Xunit.Assert.*` call into the corresponding `Expect.That(actual).<Assertion>(expected)`
form. The argument order in xUnit (`Assert.Equal(expected, actual)`) is swapped to match aweXpect's
"actual first" convention.

```csharp
// Before (xUnit)
Assert.Equal(expected, actual);
Assert.Throws<ArgumentException>(callback);

// After (aweXpect)
await Expect.That(actual).IsEqualTo(expected);
await Expect.That(callback).ThrowsExactly<ArgumentException>();
```

### Supported xUnit migrations

#### Basic

| xUnit construct                    | Rewritten to                                |
|------------------------------------|---------------------------------------------|
| `Assert.Fail("msg")`               | `Fail.Test("msg")`                          |
| `Assert.Skip("msg")`               | `Skip.Test("msg")`                          |
| `Assert.Null(subject)`             | `Expect.That(subject).IsNull()`             |
| `Assert.NotNull(subject)`          | `Expect.That(subject).IsNotNull()`          |
| `Assert.Same(expected, actual)`    | `Expect.That(actual).IsSameAs(expected)`    |
| `Assert.NotSame(expected, actual)` | `Expect.That(actual).IsNotSameAs(expected)` |

#### Booleans

| xUnit construct                  | Rewritten to                                          |
|----------------------------------|-------------------------------------------------------|
| `Assert.True(subject)`           | `Expect.That(subject).IsTrue()`                       |
| `Assert.False(subject)`          | `Expect.That(subject).IsFalse()`                      |
| `Assert.True(subject, "msg")`    | `Expect.That(subject).IsTrue().Because("msg")`        |
| `Assert.False(subject, "msg")`   | `Expect.That(subject).IsFalse().Because("msg")`       |

#### Equality

| xUnit construct                                                         | Rewritten to                                                   |
|-------------------------------------------------------------------------|----------------------------------------------------------------|
| `Assert.Equal(expected, actual)`                                        | `Expect.That(actual).IsEqualTo(expected)`                      |
| `Assert.NotEqual(expected, actual)`                                     | `Expect.That(actual).IsNotEqualTo(expected)`                   |
| `Assert.Equal(expected, actual, tolerance)` (double / float / TimeSpan) | `Expect.That(actual).IsEqualTo(expected).Within(tolerance)`    |
| `Assert.NotEqual(expected, actual, tolerance)` (double / float)         | `Expect.That(actual).IsNotEqualTo(expected).Within(tolerance)` |

#### Strings

| xUnit construct                              | Rewritten to                                          |
|----------------------------------------------|-------------------------------------------------------|
| `Assert.Contains(expected, actual)`          | `Expect.That(actual).Contains(expected)`              |
| `Assert.DoesNotContain(unexpected, actual)`  | `Expect.That(actual).DoesNotContain(unexpected)`      |
| `Assert.StartsWith(expected, actual)`        | `Expect.That(actual).StartsWith(expected)`            |
| `Assert.EndsWith(expected, actual)`          | `Expect.That(actual).EndsWith(expected)`              |
| `Assert.Empty(actual)`                       | `Expect.That(actual).IsEmpty()`                       |
| `Assert.NotEmpty(actual)`                    | `Expect.That(actual).IsNotEmpty()`                    |

#### Collections

| xUnit construct                                 | Rewritten to                                          |
|-------------------------------------------------|-------------------------------------------------------|
| `Assert.Distinct(actual)`                       | `Expect.That(actual).AreAllUnique()`                  |
| `Assert.Contains(expected, collection)`         | `Expect.That(collection).Contains(expected)`          |
| `Assert.Contains(collection, predicate)`        | `Expect.That(collection).Contains(predicate)`         |
| `Assert.DoesNotContain(unexpected, collection)` | `Expect.That(collection).DoesNotContain(unexpected)`  |
| `Assert.Empty(collection)`                      | `Expect.That(collection).IsEmpty()`                   |
| `Assert.NotEmpty(collection)`                   | `Expect.That(collection).IsNotEmpty()`                |

#### Exceptions

| xUnit construct                                                 | Rewritten to                                          |
|-----------------------------------------------------------------|-------------------------------------------------------|
| `Assert.Throws<T>(callback)` / `ThrowsAsync<T>(callback)`       | `Expect.That(callback).ThrowsExactly<T>()`            |
| `Assert.Throws(typeof(T), callback)`                            | `Expect.That(callback).ThrowsExactly(typeof(T))`      |
| `Assert.ThrowsAny<T>(callback)` / `ThrowsAnyAsync<T>(callback)` | `Expect.That(callback).Throws<T>()`                   |

#### Types

| xUnit construct                          | Rewritten to                            |
|------------------------------------------|-----------------------------------------|
| `Assert.IsAssignableFrom<T>(actual)`     | `Expect.That(actual).Is<T>()`           |
| `Assert.IsNotAssignableFrom<T>(actual)`  | `Expect.That(actual).IsNot<T>()`        |
| `Assert.IsType<T>(actual)`               | `Expect.That(actual).IsExactly<T>()`    |
| `Assert.IsNotType<T>(actual)`            | `Expect.That(actual).IsNotExactly<T>()` |

The non-generic overloads (`Assert.IsType(typeof(T), actual)` etc.) are migrated to the matching
non-generic aweXpect form.

## Removing `aweXpect.Migration` again

Once the warnings in a project have been resolved, drop the analyzer reference:

```shell
dotnet remove package aweXpect.Migration
```

`aweXpect` itself stays in the project — that's the assertion library the tests now depend on.
