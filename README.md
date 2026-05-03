# aweXpect.Migration

[![Nuget](https://img.shields.io/nuget/v/aweXpect.Migration)](https://www.nuget.org/packages/aweXpect.Migration)
[![Build](https://github.com/aweXpect/aweXpect.Migration/actions/workflows/build.yml/badge.svg)](https://github.com/aweXpect/aweXpect.Migration/actions/workflows/build.yml)
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
code fix from your IDE (Visual Studio, Rider, VS Code with C# Dev Kit) or via
`dotnet format analyzers` to rewrite the call site.

| Diagnostic     | Source library    | Code fix title                          |
|----------------|-------------------|-----------------------------------------|
| `aweXpectM002` | FluentAssertions  | *Migrate fluentassertions to aweXpect*  |
| `aweXpectM003` | xUnit             | *Migrate xunit assertion to aweXpect*   |

`aweXpectM002` is raised on every `.Should()` invocation defined under the `FluentAssertions`
namespace (nested `.Should()` inside lambda arguments are intentionally skipped, so chains like
`.Should().AllSatisfy(x => x.Should().BeGreaterThan(0))` are migrated as a single unit).
`aweXpectM003` is raised on every method call on `Xunit.Assert`.

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
`aweXpect0001` analyzer that ships with `aweXpect` itself. Run "Fix all" for that diagnostic after
the migration warnings have been applied to add the missing `await` keywords (and mark the
surrounding methods `async`).

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

Lambda-style assertions (`Action`, `Func<Task>`) are wrapped in
`aweXpect.Synchronous.Synchronously.Verify(...)` when the original call was synchronous, so existing
test signatures keep working.

### Supported FluentAssertions migrations

#### Basic

| FluentAssertions construct                    | Rewritten to                                |
|-----------------------------------------------|---------------------------------------------|
| `subject.Should().BeNull()`                   | `Expect.That(subject).IsNull()`             |
| `subject.Should().NotBeNull()`                | `Expect.That(subject).IsNotNull()`          |
| `subject.Should().BeSameAs(expected)`         | `Expect.That(subject).IsSameAs(expected)`   |
| `subject.Should().NotBeSameAs(unexpected)`    | `Expect.That(subject).IsNotSameAs(unexpected)` |
| `subject.Should().BeAssignableTo<T>()`        | `Expect.That(subject).Is<T>()`              |
| `subject.Should().NotBeAssignableTo<T>()`     | `Expect.That(subject).IsNot<T>()`           |
| `subject.Should().BeOfType<T>()`              | `Expect.That(subject).IsExactly<T>()`       |
| `subject.Should().NotBeOfType<T>()`           | `Expect.That(subject).IsNotExactly<T>()`    |
| `subject.Should().Match(predicate)`           | `Expect.That(subject).Satisfies(predicate)` |

#### Booleans

| FluentAssertions construct           | Rewritten to                          |
|--------------------------------------|---------------------------------------|
| `subject.Should().BeTrue()`          | `Expect.That(subject).IsTrue()`       |
| `subject.Should().BeFalse()`         | `Expect.That(subject).IsFalse()`      |
| `subject.Should().NotBeTrue()`       | `Expect.That(subject).IsNotTrue()`    |
| `subject.Should().NotBeFalse()`      | `Expect.That(subject).IsNotFalse()`   |
| `subject.Should().Imply(other)`      | `Expect.That(subject).Implies(other)` |

#### Numbers

| FluentAssertions construct                                    | Rewritten to                                                |
|---------------------------------------------------------------|-------------------------------------------------------------|
| `subject.Should().BePositive()`                               | `Expect.That(subject).IsPositive()`                         |
| `subject.Should().BeNegative()`                               | `Expect.That(subject).IsNegative()`                         |
| `subject.Should().BeGreaterThan(expected)`                    | `Expect.That(subject).IsGreaterThan(expected)`              |
| `subject.Should().BeGreaterThanOrEqualTo(expected)`           | `Expect.That(subject).IsGreaterThanOrEqualTo(expected)`     |
| `subject.Should().BeLessThan(expected)`                       | `Expect.That(subject).IsLessThan(expected)`                 |
| `subject.Should().BeLessThanOrEqualTo(expected)`              | `Expect.That(subject).IsLessThanOrEqualTo(expected)`        |
| `subject.Should().BeApproximately(expected, tolerance)`       | `Expect.That(subject).IsEqualTo(expected).Within(tolerance)`|
| `subject.Should().NotBeApproximately(expected, tolerance)`    | `Expect.That(subject).IsNotEqualTo(expected).Within(tolerance)` |
| `subject.Should().BeCloseTo(expected, delta)`                 | `Expect.That(subject).IsEqualTo(expected).Within(delta)`    |
| `subject.Should().NotBeCloseTo(expected, delta)`              | `Expect.That(subject).IsNotEqualTo(expected).Within(delta)` |
| `subject.Should().BeOneOf(a, b, c)`                           | `Expect.That(subject).IsOneOf(a, b, c)`                     |
| `subject.Should().BeOneOf(collection)`                        | `Expect.That(subject).IsOneOf(collection)`                  |
| `subject.Should().BeInRange(low, high)`                       | `Expect.That(subject).IsBetween(low).And(high)`             |
| `subject.Should().NotBeInRange(low, high)`                    | `Expect.That(subject).IsNotBetween(low).And(high)`          |

#### Strings

| FluentAssertions construct                                                  | Rewritten to                                                      |
|-----------------------------------------------------------------------------|-------------------------------------------------------------------|
| `subject.Should().Be(expected)`                                             | `Expect.That(subject).IsEqualTo(expected)`                        |
| `subject.Should().NotBe(unexpected)`                                        | `Expect.That(subject).IsNotEqualTo(unexpected)`                   |
| `subject.Should().BeEquivalentTo(expected)`                                 | `Expect.That(subject).IsEqualTo(expected).IgnoringCase()`         |
| `subject.Should().BeEquivalentTo(expected, o => o.IgnoringLeadingWhitespace())` | `Expect.That(subject).IsEqualTo(expected).IgnoringCase().IgnoringLeadingWhiteSpace()` |
| `subject.Should().BeEquivalentTo(expected, o => o.IgnoringTrailingWhitespace())` | `Expect.That(subject).IsEqualTo(expected).IgnoringCase().IgnoringTrailingWhiteSpace()` |
| `subject.Should().BeEquivalentTo(expected, o => o.IgnoringNewlineStyle())`  | `Expect.That(subject).IsEqualTo(expected).IgnoringCase().IgnoringNewlineStyle()` |
| `subject.Should().Match(pattern)`                                           | `Expect.That(subject).IsEqualTo(pattern).AsWildcard()`            |
| `subject.Should().BeEmpty()`                                                | `Expect.That(subject).IsEmpty()`                                  |
| `subject.Should().NotBeEmpty()`                                             | `Expect.That(subject).IsNotEmpty()`                               |
| `subject.Should().BeNullOrEmpty()`                                          | `Expect.That(subject).IsNullOrEmpty()`                            |
| `subject.Should().NotBeNullOrEmpty()`                                       | `Expect.That(subject).IsNotNullOrEmpty()`                         |
| `subject.Should().BeNullOrWhiteSpace()`                                     | `Expect.That(subject).IsNullOrWhiteSpace()`                       |
| `subject.Should().NotBeNullOrWhiteSpace()`                                  | `Expect.That(subject).IsNotNullOrWhiteSpace()`                    |
| `subject.Should().Contain(expected)`                                        | `Expect.That(subject).Contains(expected)`                         |
| `subject.Should().Contain(expected, AtLeast.Once())`                        | `Expect.That(subject).Contains(expected).AtLeast().Once()`        |
| `subject.Should().Contain(expected, Exactly.Twice())`                       | `Expect.That(subject).Contains(expected).Twice()`                 |
| `subject.Should().Contain(expected, AtMost.Times(n))`                       | `Expect.That(subject).Contains(expected).AtMost(n)`               |
| `subject.Should().NotContain(unexpected)`                                   | `Expect.That(subject).DoesNotContain(unexpected)`                 |
| `subject.Should().StartWith(expected)`                                      | `Expect.That(subject).StartsWith(expected)`                       |
| `subject.Should().NotStartWith(unexpected)`                                 | `Expect.That(subject).DoesNotStartWith(unexpected)`               |
| `subject.Should().EndWith(expected)`                                        | `Expect.That(subject).EndsWith(expected)`                         |
| `subject.Should().NotEndWith(unexpected)`                                   | `Expect.That(subject).DoesNotEndWith(unexpected)`                 |

The same `AtLeast` / `AtMost` / `Exactly` / `LessThan` / `MoreThan` mapping is applied for the
`.Once()` / `.Twice()` / `.Thrice()` / `.Times(n)` overloads.

#### Collections

| FluentAssertions construct                                       | Rewritten to                                                                |
|------------------------------------------------------------------|-----------------------------------------------------------------------------|
| `subject.Should().HaveCount(n)`                                  | `Expect.That(subject).HasCount(n)`                                          |
| `subject.Should().BeEquivalentTo(expected)`                      | `Expect.That(subject).IsEqualTo(expected).InAnyOrder()`                     |
| `subject.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering())` | `Expect.That(subject).IsEqualTo(expected)`                          |
| `subject.Should().BeEquivalentTo(expected, o => o.WithoutStrictOrdering())` | `Expect.That(subject).IsEqualTo(expected).InAnyOrder()`          |
| `subject.Should().NotBeEquivalentTo(unexpected)`                 | `Expect.That(subject).IsNotEqualTo(unexpected).InAnyOrder()`                |
| `subject.Should().OnlyContain(predicate)`                        | `Expect.That(subject).All().Satisfy(predicate)`                             |
| `subject.Should().ContainSingle()`                               | `Expect.That(subject).HasSingle()`                                          |
| `subject.Should().ContainSingle(predicate)`                      | `Expect.That(subject).HasSingle().Matching(predicate)`                      |
| `subject.Should().Contain(item).And.Contain(other)`              | `Expect.That(subject).Contains(item).And.Contains(other)`                   |
| `subject.Should().Contain(collection)`                           | `Expect.That(subject).Contains(collection).InAnyOrder().IgnoringInterspersedItems()` |
| `subject.Should().ContainInOrder(collection)`                    | `Expect.That(subject).Contains(collection).IgnoringInterspersedItems()`     |
| `subject.Should().ContainInConsecutiveOrder(collection)`         | `Expect.That(subject).Contains(collection)`                                 |
| `subject.Should().ContainEquivalentOf(expected)`                 | `Expect.That(subject).Contains(expected).Equivalent()`                      |
| `subject.Should().NotContainEquivalentOf(unexpected)`            | `Expect.That(subject).DoesNotContain(unexpected).Equivalent()`              |
| `subject.Should().StartWith(collection)`                         | `Expect.That(subject).StartsWith(collection)`                               |
| `subject.Should().EndWith(collection)`                           | `Expect.That(subject).EndsWith(collection)`                                 |
| `subject.Should().BeSubsetOf(expected)`                          | `Expect.That(subject).IsContainedIn(expected).InAnyOrder()`                 |
| `subject.Should().NotBeSubsetOf(expected)`                       | `Expect.That(subject).IsNotContainedIn(expected).InAnyOrder()`              |
| `subject.Should().AllBeAssignableTo<T>()`                        | `Expect.That(subject).All().Are<T>()`                                       |
| `subject.Should().AllBeOfType<T>()`                              | `Expect.That(subject).All().AreExactly<T>()`                                |
| `subject.Should().AllBeEquivalentTo(expected)`                   | `Expect.That(subject).All().AreEquivalentTo(expected)`                      |
| `subject.Should().BeInAscendingOrder()`                          | `Expect.That(subject).IsInAscendingOrder()`                                 |
| `subject.Should().BeInAscendingOrder(comparer)`                  | `Expect.That(subject).IsInAscendingOrder().Using(comparer)`                 |
| `subject.Should().BeInAscendingOrder(x => x.Key)`                | `Expect.That(subject).IsInAscendingOrder(x => x.Key)`                       |
| `subject.Should().BeInDescendingOrder()`                         | `Expect.That(subject).IsInDescendingOrder()`                                |
| `subject.Should().NotBeInAscendingOrder()` / `NotBeInDescendingOrder()` | `Expect.That(subject).IsNotInAscendingOrder()` / `IsNotInDescendingOrder()` |
| `subject.Should().OnlyHaveUniqueItems()`                         | `Expect.That(subject).AreAllUnique()`                                       |

#### Equivalency

| FluentAssertions construct                                | Rewritten to                                          |
|-----------------------------------------------------------|-------------------------------------------------------|
| `subject.Should().BeEquivalentTo(expected)` (object)      | `Expect.That(subject).IsEquivalentTo(expected)`       |
| `subject.Should().NotBeEquivalentTo(unexpected)` (object) | `Expect.That(subject).IsNotEquivalentTo(unexpected)`  |

#### Exceptions

| FluentAssertions construct                              | Rewritten to                                                        |
|---------------------------------------------------------|---------------------------------------------------------------------|
| `callback.Should().NotThrow()` / `NotThrowAsync()`      | `Expect.That(callback).DoesNotThrow()`                              |
| `callback.Should().NotThrow<T>()` / `NotThrowAsync<T>()`| `Expect.That(callback).DoesNotThrow<T>()`                           |
| `callback.Should().Throw<T>()` / `ThrowAsync<T>()`      | `Expect.That(callback).Throws<T>()`                                 |
| `callback.Should().ThrowExactly<T>()` / `ThrowExactlyAsync<T>()` | `Expect.That(callback).ThrowsExactly<T>()`                  |
| `…WithMessage(pattern)`                                 | `…WithMessage(pattern).AsWildcard()`                                |

#### Dates and times

| FluentAssertions construct                                | Rewritten to                                          |
|-----------------------------------------------------------|-------------------------------------------------------|
| `subject.Should().BeAfter(expected)`                      | `Expect.That(subject).IsAfter(expected)`              |
| `subject.Should().BeOnOrAfter(expected)`                  | `Expect.That(subject).IsOnOrAfter(expected)`          |
| `subject.Should().BeBefore(expected)`                     | `Expect.That(subject).IsBefore(expected)`             |
| `subject.Should().BeOnOrBefore(expected)`                 | `Expect.That(subject).IsOnOrBefore(expected)`         |
| `subject.Should().NotBeAfter(unexpected)` / `NotBeOnOrAfter(...)` | `Expect.That(subject).IsNotAfter(unexpected)` / `IsNotOnOrAfter(...)` |
| `subject.Should().NotBeBefore(unexpected)` / `NotBeOnOrBefore(...)` | `Expect.That(subject).IsNotBefore(unexpected)` / `IsNotOnOrBefore(...)` |
| `subject.Should().HaveYear(n)` (also `Month`/`Day`/`Hour`/`Minute`/`Second`/`Offset`) | `Expect.That(subject).HasYear().EqualTo(n)` (etc.) |
| `subject.Should().NotHaveYear(n)` (and the other parts)   | `Expect.That(subject).HasYear().NotEqualTo(n)` (etc.) |

#### Enums and nullable values

| FluentAssertions construct                  | Rewritten to                                  |
|---------------------------------------------|-----------------------------------------------|
| `subject.Should().BeDefined()`              | `Expect.That(subject).IsDefined()`            |
| `subject.Should().NotBeDefined()`           | `Expect.That(subject).IsNotDefined()`         |
| `subject.Should().HaveFlag(flag)`           | `Expect.That(subject).HasFlag(flag)`          |
| `subject.Should().NotHaveFlag(flag)`        | `Expect.That(subject).DoesNotHaveFlag(flag)`  |
| `subject.Should().HaveValue()` (`Nullable<T>`) | `Expect.That(subject).IsNotNull()`         |
| `subject.Should().HaveValue(v)`             | `Expect.That(subject).HasValue(v)`            |
| `subject.Should().NotHaveValue()`           | `Expect.That(subject).IsNull()`               |
| `subject.Should().NotHaveValue(v)`          | `Expect.That(subject).DoesNotHaveValue(v)`    |

#### Because messages

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

| xUnit construct                  | Rewritten to                                |
|----------------------------------|---------------------------------------------|
| `Assert.Fail("msg")`             | `Fail.Test("msg")`                          |
| `Assert.Skip("msg")`             | `Skip.Test("msg")`                          |
| `Assert.Null(subject)`           | `Expect.That(subject).IsNull()`             |
| `Assert.NotNull(subject)`        | `Expect.That(subject).IsNotNull()`          |
| `Assert.Same(expected, actual)`  | `Expect.That(actual).IsSameAs(expected)`    |
| `Assert.NotSame(expected, actual)` | `Expect.That(actual).IsNotSameAs(expected)` |

#### Booleans

| xUnit construct                  | Rewritten to                                          |
|----------------------------------|-------------------------------------------------------|
| `Assert.True(subject)`           | `Expect.That(subject).IsTrue()`                       |
| `Assert.False(subject)`          | `Expect.That(subject).IsFalse()`                      |
| `Assert.True(subject, "msg")`    | `Expect.That(subject).IsTrue().Because("msg")`        |
| `Assert.False(subject, "msg")`   | `Expect.That(subject).IsFalse().Because("msg")`       |

#### Equality

| xUnit construct                                       | Rewritten to                                                |
|-------------------------------------------------------|-------------------------------------------------------------|
| `Assert.Equal(expected, actual)`                      | `Expect.That(actual).IsEqualTo(expected)`                   |
| `Assert.NotEqual(expected, actual)`                   | `Expect.That(actual).IsNotEqualTo(expected)`                |
| `Assert.Equal(expected, actual, tolerance)` (double / float / TimeSpan) | `Expect.That(actual).IsEqualTo(expected).Within(tolerance)` |
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
