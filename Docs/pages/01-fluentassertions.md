---
description: The FluentAssertions assertions that aweXpect.Migration rewrites to aweXpect.
---

import ApplyFix from '@site/src/components/ApplyFix';

# FluentAssertions

The code fix of `aweXpectM002` rewrites the entire `.Should()` chain in one step: the `Should()` call is dropped, `Expect.That(subject)` becomes the new root, the assertion methods are renamed, any trailing `because` argument is moved into a `.Because(...)` suffix, and `.And` chains are preserved.

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
string title = // ...
string[] tracks = // ...
int playCount = // ...
double rating = // ...
DateTime releaseDate = // ...
Action play = // ...

// highlight-start
title.Should().NotBeNullOrEmpty();
title.Should().StartWith("Abbey", "the album title was set");
tracks.Should().HaveCount(17).And.Contain("Come Together");
tracks.Should().ContainInOrder("Something", "Octopus's Garden");
tracks.Should().OnlyHaveUniqueItems();
playCount.Should().BeGreaterThan(0);
rating.Should().BeApproximately(4.5, 0.1);
releaseDate.Should().HaveYear(1969);
play.Should().Throw<InvalidOperationException>().WithMessage("*no tracks*");
// highlight-end
```

```csharp title="After (aweXpect)"
string title = // ...
string[] tracks = // ...
int playCount = // ...
double rating = // ...
DateTime releaseDate = // ...
Action play = // ...

// highlight-start
Expect.That(title).IsNotNullOrEmpty();
Expect.That(title).StartsWith("Abbey").Because("the album title was set");
Expect.That(tracks).HasCount(17).And.Contains("Come Together");
Expect.That(tracks).Contains(["Something", "Octopus's Garden"]).IgnoringInterspersedItems();
Expect.That(tracks).All().AreUnique();
Expect.That(playCount).IsGreaterThan(0);
Expect.That(rating).IsEqualTo(4.5).Within(0.1);
Expect.That(releaseDate).HasYear().EqualTo(1969);
Expect.That(play).Throws<InvalidOperationException>().WithMessage("*no tracks*").AsWildcard().IgnoringCase().IgnoringNewlineStyle();
// highlight-end
```

</ApplyFix>

The examples show the code as the code fix writes it; the `await` is added in a [second step](./00-index.md#recommended-workflow).

The tables below are grouped by **target concept** (equality, containment and so on), not by the FluentAssertions documentation pages. Most of the basic assertions (`Be`, `NotBe`, `BeEmpty`, `Contain`, `StartWith` and others) are not specific to one subject type; they apply to any compatible subject.

A chain that continues on a second `.Should()`, e.g. after `.Which`, is left for manual migration, as is any call the code fix cannot translate without losing an argument (see [Manual review](./00-index.md#manual-review)).

A note in the last column marks a rewrite that behaves differently in some cases; it links to the matching entry on [Behavioral differences from FluentAssertions](./02-fluentassertions-differences.md).

## Equality

| FluentAssertions construct                                  | Rewritten to                                                    | Note                                                                                                         |
|-------------------------------------------------------------|-----------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| `subject.Should().Be(expected)`                             | `Expect.That(subject).IsEqualTo(expected)`                      | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds)                                                                             |
| `subject.Should().NotBe(unexpected)`                        | `Expect.That(subject).IsNotEqualTo(unexpected)`                 | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds)                                                                             |
| `subject.Should().BeApproximately(expected, tolerance)`     | `Expect.That(subject).IsEqualTo(expected).Within(tolerance)`    | [NaN](./02-fluentassertions-differences.md#nan-is-an-ordinary-value)                                                                             |
| `subject.Should().NotBeApproximately(expected, tolerance)`  | `Expect.That(subject).IsNotEqualTo(expected).Within(tolerance)` | [NaN](./02-fluentassertions-differences.md#nan-is-an-ordinary-value)                                                                             |
| `subject.Should().BeCloseTo(expected, delta)`               | `Expect.That(subject).IsEqualTo(expected).Within(delta)`        | [integral deltas](./02-fluentassertions-differences.md#integral-tolerances), [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds)                                    |
| `subject.Should().NotBeCloseTo(expected, delta)`            | `Expect.That(subject).IsNotEqualTo(expected).Within(delta)`     | [integral deltas](./02-fluentassertions-differences.md#integral-tolerances), [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds)                                    |
| `subject.Should().BeOneOf(a, b, c)`                         | `Expect.That(subject).IsOneOf(a, b, c)`                         | [mixed number types](./02-fluentassertions-differences.md#numbers-of-different-types), [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds)                          |
| `subject.Should().BeOneOf(collection)`                      | `Expect.That(subject).IsOneOf(collection)`                      | [mixed number types](./02-fluentassertions-differences.md#numbers-of-different-types), [empty expectations](./02-fluentassertions-differences.md#empty-expectations-throw), [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds) |
| `subject.Should().BeSameAs(expected)`                       | `Expect.That(subject).IsSameAs(expected)`                       |                                                                                                              |
| `subject.Should().NotBeSameAs(unexpected)`                  | `Expect.That(subject).IsNotSameAs(unexpected)`                  |                                                                                                              |

For an integral subject, FluentAssertions takes the delta of `BeCloseTo` as the unsigned counterpart of the subject type (`uint` for `int`). When the delta does not convert implicitly, the code fix casts it to the subject type, e.g. `Within((int)delta)`. A constant delta that does not fit the subject type, such as `uint.MaxValue` for an `int`, is left for manual migration.

`Be`, `NotBe` and `BeOneOf` with an `IEqualityComparer<T>` are left for manual migration as well. FluentAssertions fails for a subject that is not a `T`, such as `null`, without calling the comparer, whereas `Using(comparer)` in aweXpect passes every subject to the comparer.

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
double rating = // ...
DateTime releaseDate = // ...
DateTime expected = // ...

// highlight-start
rating.Should().BeApproximately(4.5, 0.1);
releaseDate.Should().BeCloseTo(expected, TimeSpan.FromDays(1));
// highlight-end
```

```csharp title="After (aweXpect)"
double rating = // ...
DateTime releaseDate = // ...
DateTime expected = // ...

// highlight-start
Expect.That(rating).IsEqualTo(4.5).Within(0.1);
Expect.That(releaseDate).IsEqualTo(expected).Within(TimeSpan.FromDays(1));
// highlight-end
```

</ApplyFix>

### Equivalency

`Should().BeEquivalentTo(...)` and `Should().NotBeEquivalentTo(...)` dispatch on the called overload and the type of the expected items:

| Subject                                                          | `BeEquivalentTo(expected)` rewrites to                       |
|------------------------------------------------------------------|--------------------------------------------------------------|
| `string`                                                         | `IsEqualTo(expected).IgnoringCase()`                         |
| collection of items that are compared by value (see below)       | `IsEqualTo(expected).InAnyOrder()`                           |
| object that is compared by value (see below)                     | `IsEqualTo(expected)`                                        |
| any other collection, and any other object                       | `IsEquivalentTo(expected, o => o.IgnoringCollectionOrder())` |

Items are compared by value when FluentAssertions compares them with `Equals` as well: primitives, enums, strings, structs and classes that override `Equals`, but not records and anonymous types. All other items are compared structurally, like FluentAssertions does. FluentAssertions also ignores the order of nested collections, which `IgnoringCollectionOrder()` restores; the code fix adds `using aweXpect.Equivalency;` for it when necessary. With `o => o.WithStrictOrdering()` the rewrite omits `InAnyOrder()` and `IgnoringCollectionOrder()`.

Unlike a single string, the strings in a collection are compared case-sensitively, as in FluentAssertions. The expected strings can also be listed one by one: `BeEquivalentTo("Something", "Come Together")` rewrites to `IsEqualTo(["Something", "Come Together"]).InAnyOrder()`. Without any expected string (`BeEquivalentTo()` or `BeEquivalentTo([])`) no code fix is offered.

For string subjects and collections of items that are compared by value, the `o => o...` options are translated as well. As in FluentAssertions, a string compared with options ignores case only when the options contain `IgnoringCase()`:

| FluentAssertions option               | Appended to the rewrite         |
|---------------------------------------|---------------------------------|
| `o => o.IgnoringCase()`               | `.IgnoringCase()`               |
| `o => o.IgnoringLeadingWhitespace()`  | `.IgnoringLeadingWhiteSpace()`  |
| `o => o.IgnoringTrailingWhitespace()` | `.IgnoringTrailingWhiteSpace()` |
| `o => o.IgnoringNewlineStyle()`       | `.IgnoringNewlineStyle()`       |

For these, the code fix translates only an `o => o...` lambda that chains the options above, `WithStrictOrdering()` and `WithoutStrictOrdering()`. For structural comparisons and objects that are compared by value, options other than `WithStrictOrdering()` and `WithoutStrictOrdering()` have no automatic translation. Any other option, such as `Using<T>(...)`, or options that are not passed as such a lambda, would be lost in the rewrite, so no code fix is offered. `NotBeEquivalentTo` is translated symmetrically (`IsNotEqualTo` / `IsNotEquivalentTo`). See also [Structural equivalency](./02-fluentassertions-differences.md#structural-equivalency) under the behavioral differences.

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
string title = // ...
string[] tracks = // ...
string[] expected = // ...
object album = // ...

// highlight-start
title.Should().BeEquivalentTo("abbey road");
tracks.Should().BeEquivalentTo(expected);
album.Should().BeEquivalentTo(new { Title = "Abbey Road", Year = 1969 });
// highlight-end
```

```csharp title="After (aweXpect)"
// highlight-next-line
using aweXpect.Equivalency;

string title = // ...
string[] tracks = // ...
string[] expected = // ...
object album = // ...

// highlight-start
Expect.That(title).IsEqualTo("abbey road").IgnoringCase();
Expect.That(tracks).IsEqualTo(expected).InAnyOrder();
Expect.That(album).IsEquivalentTo(new { Title = "Abbey Road", Year = 1969 }, o => o.IgnoringCollectionOrder());
// highlight-end
```

</ApplyFix>

## Null and emptiness

| FluentAssertions construct                              | Rewritten to                                     | Note                                                 |
|---------------------------------------------------------|--------------------------------------------------|------------------------------------------------------|
| `subject.Should().BeNull()`                             | `Expect.That(subject).IsNull()`                  |                                                      |
| `subject.Should().NotBeNull()`                          | `Expect.That(subject).IsNotNull()`               |                                                      |
| `subject.Should().BeEmpty()`                            | `Expect.That(subject).IsEmpty()`                 |                                                      |
| `subject.Should().NotBeEmpty()`                         | `Expect.That(subject).IsNotEmpty()`              | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null)  |
| `subject.Should().BeNullOrEmpty()` *(string)*           | `Expect.That(subject).IsNullOrEmpty()`           |                                                      |
| `subject.Should().BeNullOrEmpty()` *(collection)*       | `Expect.That(subject).IsNull().Or.IsEmpty()`     |                                                      |
| `subject.Should().NotBeNullOrEmpty()` *(string)*        | `Expect.That(subject).IsNotNullOrEmpty()`        |                                                      |
| `subject.Should().NotBeNullOrEmpty()` *(collection)*    | `Expect.That(subject).IsNotEmpty()`              |                                                      |
| `subject.Should().BeNullOrWhiteSpace()`                 | `Expect.That(subject).IsNullOrWhiteSpace()`      |                                                      |
| `subject.Should().NotBeNullOrWhiteSpace()`              | `Expect.That(subject).IsNotNullOrWhiteSpace()`   |                                                      |

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
string[] bonusTracks = // ...
string[] tracks = // ...

// highlight-start
bonusTracks.Should().BeNullOrEmpty();
tracks.Should().NotBeNullOrEmpty();
// highlight-end
```

```csharp title="After (aweXpect)"
string[] bonusTracks = // ...
string[] tracks = // ...

// highlight-start
Expect.That(bonusTracks).IsNull().Or.IsEmpty();
Expect.That(tracks).IsNotEmpty();
// highlight-end
```

</ApplyFix>

## Booleans

| FluentAssertions construct      | Rewritten to                          |
|---------------------------------|---------------------------------------|
| `subject.Should().BeTrue()`     | `Expect.That(subject).IsTrue()`       |
| `subject.Should().BeFalse()`    | `Expect.That(subject).IsFalse()`      |
| `subject.Should().NotBeTrue()`  | `Expect.That(subject).IsNotTrue()`    |
| `subject.Should().NotBeFalse()` | `Expect.That(subject).IsNotFalse()`   |
| `subject.Should().Imply(other)` | `Expect.That(subject).Implies(other)` |

## Type checks

The generic forms (`<T>`) and the non-generic `(typeof(T))` overloads are both supported; the code fix keeps whichever form was used.

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

On a `System.Type` subject, `BeAssignableTo` and `NotBeAssignableTo` are not rewritten; see [Assertions on a `Type`](./02-fluentassertions-differences.md#assertions-on-a-type).

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
Exception error = // ...

// highlight-start
error.Should().BeAssignableTo<ArgumentException>();
error.Should().BeOfType<ArgumentNullException>();
// highlight-end
```

```csharp title="After (aweXpect)"
Exception error = // ...

// highlight-start
Expect.That(error).Is<ArgumentException>();
Expect.That(error).IsExactly<ArgumentNullException>();
// highlight-end
```

</ApplyFix>

## Numbers

| FluentAssertions construct                                                    | Rewritten to                                            | Note                                      |
|-------------------------------------------------------------------------------|---------------------------------------------------------|-------------------------------------------|
| `subject.Should().BePositive()`                                               | `Expect.That(subject).IsPositive()`                     |                                           |
| `subject.Should().BeNegative()`                                               | `Expect.That(subject).IsNegative()`                     |                                           |
| `subject.Should().BeGreaterThan(expected)`                                    | `Expect.That(subject).IsGreaterThan(expected)`          |                                           |
| `subject.Should().BeGreaterThanOrEqualTo(expected)`                           | `Expect.That(subject).IsGreaterThanOrEqualTo(expected)` |                                           |
| `subject.Should().BeGreaterOrEqualTo(expected)` *(legacy FluentAssertions 7)* | `Expect.That(subject).IsGreaterThanOrEqualTo(expected)` |                                           |
| `subject.Should().BeLessThan(expected)`                                       | `Expect.That(subject).IsLessThan(expected)`             |                                           |
| `subject.Should().BeLessThanOrEqualTo(expected)`                              | `Expect.That(subject).IsLessThanOrEqualTo(expected)`    |                                           |
| `subject.Should().BeLessOrEqualTo(expected)` *(legacy FluentAssertions 7)*    | `Expect.That(subject).IsLessThanOrEqualTo(expected)`    |                                           |
| `subject.Should().BeInRange(low, high)`                                       | `Expect.That(subject).IsBetween(low).And(high)`         | [reversed ranges](./02-fluentassertions-differences.md#reversed-ranges-throw)   |
| `subject.Should().NotBeInRange(low, high)`                                    | `Expect.That(subject).IsNotBetween(low).And(high)`      | [reversed ranges](./02-fluentassertions-differences.md#reversed-ranges-throw)   |

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
int rating = // ...

// highlight-next-line
rating.Should().BeInRange(1, 5);
```

```csharp title="After (aweXpect)"
int rating = // ...

// highlight-next-line
Expect.That(rating).IsBetween(1).And(5);
```

</ApplyFix>

## Containment, ordering and predicates

These cover both string and collection subjects; the code fix dispatches on the called overload where it matters, so an item that is itself a collection is still treated as a single item.

| FluentAssertions construct                                              | Rewritten to                                                                                              | Note                                                 |
|-------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|------------------------------------------------------|
| `subject.Should().Contain(expected)`                                    | `Expect.That(subject).Contains(expected)`                                                                 |                                                      |
| `subject.Should().Contain(item).And.Contain(other)`                     | `Expect.That(subject).Contains(item).And.Contains(other)`                                                 |                                                      |
| `subject.Should().Contain(collection)`                                  | `Expect.That(subject).Contains(collection).InAnyOrder().IgnoringDuplicates()`                             |                                                      |
| `subject.Should().NotContain(unexpected)`                               | `Expect.That(subject).DoesNotContain(unexpected)`                                                         | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null)  |
| `subject.Should().NotContain(collection)`                               | `Expect.That(subject).None().ComplyWith(x => x.IsOneOf(collection))`                                      | [mixed number types](./02-fluentassertions-differences.md#numbers-of-different-types)    |
| `subject.Should().StartWith(expected)`                                  | `Expect.That(subject).StartsWith(expected)`                                                               | [empty expectations](./02-fluentassertions-differences.md#empty-expectations-throw)      |
| `subject.Should().NotStartWith(unexpected)`                             | `Expect.That(subject).DoesNotStartWith(unexpected)`                                                       | [empty expectations](./02-fluentassertions-differences.md#empty-expectations-throw)      |
| `subject.Should().EndWith(expected)`                                    | `Expect.That(subject).EndsWith(expected)`                                                                 | [empty expectations](./02-fluentassertions-differences.md#empty-expectations-throw)      |
| `subject.Should().NotEndWith(unexpected)`                               | `Expect.That(subject).DoesNotEndWith(unexpected)`                                                         | [empty expectations](./02-fluentassertions-differences.md#empty-expectations-throw)      |
| `subject.Should().ContainInOrder(collection)`                           | `Expect.That(subject).Contains(collection).IgnoringInterspersedItems()`                                   | [empty expectations](./02-fluentassertions-differences.md#empty-expectations-throw)      |
| `subject.Should().ContainInOrder(a, b, c)`                              | `Expect.That(subject).Contains([a, b, c]).IgnoringInterspersedItems()`                                    |                                                      |
| `subject.Should().ContainInConsecutiveOrder(collection)`                | `Expect.That(subject).Contains(collection)`                                                               | [empty expectations](./02-fluentassertions-differences.md#empty-expectations-throw)      |
| `subject.Should().ContainInConsecutiveOrder(a, b, c)`                   | `Expect.That(subject).Contains([a, b, c])`                                                                |                                                      |
| `subject.Should().ContainEquivalentOf(expected)`                        | `Expect.That(subject).Contains(expected).Equivalent(o => o.IgnoringCollectionOrder())`                    | [structural equivalency](./02-fluentassertions-differences.md#structural-equivalency)    |
| `subject.Should().NotContainEquivalentOf(unexpected)`                   | `Expect.That(subject).DoesNotContain(unexpected).Equivalent(o => o.IgnoringCollectionOrder())`            | [structural equivalency](./02-fluentassertions-differences.md#structural-equivalency)    |
| `subject.Should().BeSubsetOf(expected)`                                 | `Expect.That(subject).IsContainedIn(expected).InAnyOrder().IgnoringDuplicates()`                          |                                                      |
| `subject.Should().NotBeSubsetOf(expected)`                              | `Expect.That(subject).IsNotContainedIn(expected).InAnyOrder()`                                            |                                                      |
| `subject.Should().HaveCount(n)`                                         | `Expect.That(subject).HasCount(n)`                                                                        |                                                      |
| `subject.Should().ContainSingle()`                                      | `Expect.That(subject).HasSingle()`                                                                        |                                                      |
| `subject.Should().ContainSingle(predicate)`                             | `Expect.That(subject).HasSingle().Matching(predicate)`                                                    |                                                      |
| `subject.Should().OnlyContain(predicate)`                               | `Expect.That(subject).All().Satisfy(predicate)`                                                           |                                                      |
| `subject.Should().OnlyHaveUniqueItems()`                                | `Expect.That(subject).All().AreUnique()`                                                                  |                                                      |
| `subject.Should().BeInAscendingOrder()`                                 | `Expect.That(subject).IsInAscendingOrder()`                                                               |                                                      |
| `subject.Should().BeInAscendingOrder(comparer)`                         | `Expect.That(subject).IsInAscendingOrder().Using(comparer)`                                               |                                                      |
| `subject.Should().BeInAscendingOrder(x => x.Key)`                       | `Expect.That(subject).IsInAscendingOrder(x => x.Key)`                                                     |                                                      |
| `subject.Should().BeInDescendingOrder()`                                | `Expect.That(subject).IsInDescendingOrder()`                                                              |                                                      |
| `subject.Should().NotBeInAscendingOrder()` / `NotBeInDescendingOrder()` | `Expect.That(subject).IsNotInAscendingOrder()` / `IsNotInDescendingOrder()`                               |                                                      |
| `subject.Should().Match(predicate)` *(non-string subject)*              | `Expect.That(subject).Satisfies(predicate)`                                                               |                                                      |
| `subject.Should().Match(pattern)` *(string subject)*                    | `Expect.That(subject).IsEqualTo(pattern).AsWildcard()`                                                    | [empty patterns](./02-fluentassertions-differences.md#empty-wildcard-patterns)           |

For items that are compared by value (see [Equivalency](#equivalency)), `ContainEquivalentOf` and `NotContainEquivalentOf` are rewritten to `Contains(expected)` and `DoesNotContain(unexpected)` without `Equivalent()`. With `o => o.WithStrictOrdering()` they use `Equivalent()` without options.

Items passed one by one to `ContainInOrder` or `ContainInConsecutiveOrder` become a collection expression, also a single item: `ContainInOrder(a)` is rewritten to `Contains([a]).IgnoringInterspersedItems()`.

The ordering assertions with a comparison delegate, such as `BeInAscendingOrder((a, b) => a.CompareTo(b))`, are left for manual migration, because `Using` in aweXpect takes an `IComparer<T>`. Wrap the delegate in `Comparer<T>.Create(...)`.

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
string[] tracks = // ...
string[] favorites = // ...

// highlight-start
tracks.Should().Contain(favorites);
tracks.Should().ContainInOrder("Something", "Octopus's Garden");
// highlight-end
```

```csharp title="After (aweXpect)"
string[] tracks = // ...
string[] favorites = // ...

// highlight-start
Expect.That(tracks).Contains(favorites).InAnyOrder().IgnoringDuplicates();
Expect.That(tracks).Contains(["Something", "Octopus's Garden"]).IgnoringInterspersedItems();
// highlight-end
```

</ApplyFix>

### Occurrence constraints on `Contain` (string subjects)

`Should().Contain(expected, occurrence)` translates the full FluentAssertions occurrence family. The `Thrice` and `Times(n)` cases are emitted as numeric arguments, not as a chained `.Thrice()`. Occurrences are [counted without overlap](./02-fluentassertions-differences.md#occurrences-are-counted-without-overlap).

| FluentAssertions occurrence | Appended to the rewrite | Note                                                |
|-----------------------------|-------------------------|-----------------------------------------------------|
| `AtLeast.Once()`            | `.AtLeast().Once()`     |                                                     |
| `AtLeast.Twice()`           | `.AtLeast().Twice()`    |                                                     |
| `AtLeast.Thrice()`          | `.AtLeast(3)`           |                                                     |
| `AtLeast.Times(n)`          | `.AtLeast(n)`           |                                                     |
| `AtMost.Once()`             | `.AtMost().Once()`      | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null) |
| `AtMost.Twice()`            | `.AtMost().Twice()`     | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null) |
| `AtMost.Thrice()`           | `.AtMost(3)`            | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null) |
| `AtMost.Times(n)`           | `.AtMost(n)`            | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null) |
| `Exactly.Once()`            | `.Once()`               |                                                     |
| `Exactly.Twice()`           | `.Twice()`              |                                                     |
| `Exactly.Thrice()`          | `.Exactly(3)`           |                                                     |
| `Exactly.Times(n)`          | `.Exactly(n)`           |                                                     |
| `LessThan.Twice()`          | `.LessThan().Twice()`   | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null) |
| `LessThan.Thrice()`         | `.LessThan(3)`          | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null) |
| `LessThan.Times(n)`         | `.LessThan(n)`          | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null) |
| `MoreThan.Once()`           | `.MoreThan().Once()`    |                                                     |
| `MoreThan.Twice()`          | `.MoreThan().Twice()`   |                                                     |
| `MoreThan.Thrice()`         | `.MoreThan(3)`          |                                                     |
| `MoreThan.Times(n)`         | `.MoreThan(n)`          |                                                     |

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
string lyrics = // ...

// highlight-start
lyrics.Should().Contain("love", AtLeast.Twice());
lyrics.Should().Contain("yesterday", Exactly.Once());
lyrics.Should().Contain("help", AtMost.Thrice());
// highlight-end
```

```csharp title="After (aweXpect)"
string lyrics = // ...

// highlight-start
Expect.That(lyrics).Contains("love").AtLeast().Twice();
Expect.That(lyrics).Contains("yesterday").Once();
Expect.That(lyrics).Contains("help").AtMost(3);
// highlight-end
```

</ApplyFix>

## Collections

In addition to the containment and ordering assertions above, these collection-specific forms are recognized:

| FluentAssertions construct                                               | Rewritten to                                                                             | Note                                              |
|--------------------------------------------------------------------------|------------------------------------------------------------------------------------------|---------------------------------------------------|
| `subject.Should().AllBeAssignableTo<T>()` / `(typeof(T))`                | `Expect.That(subject).All().Are<T>()` / `All().Are(typeof(T))`                           | [Type items](./02-fluentassertions-differences.md#type-instances-as-items)            |
| `subject.Should().AllBeOfType<T>()` / `(typeof(T))`                      | `Expect.That(subject).All().AreExactly<T>()` / `All().AreExactly(typeof(T))`             | [Type items](./02-fluentassertions-differences.md#type-instances-as-items)            |
| `subject.Should().AllBeEquivalentTo(expected)` *(non-string elements)*   | `Expect.That(subject).All().AreEquivalentTo(expected, o => o.IgnoringCollectionOrder())` | [structural equivalency](./02-fluentassertions-differences.md#structural-equivalency) |
| `subject.Should().AllBeEquivalentTo(expected)` *(`IEnumerable<string>`)* | `Expect.That(subject).All().AreEqualTo(expected)`                                        |                                                   |

For elements that are compared by value (see [Equivalency](#equivalency)), `AllBeEquivalentTo` is rewritten to `All().AreEqualTo(expected)`; with `o => o.WithStrictOrdering()` it is rewritten to `All().AreEquivalentTo(expected)` without the options lambda. For `IEnumerable<string>` collections, the `o => o...` options are translated as well:

| FluentAssertions option               | Appended to the rewrite         |
|---------------------------------------|---------------------------------|
| `o => o.IgnoringCase()`               | `.IgnoringCase()`               |
| `o => o.IgnoringLeadingWhitespace()`  | `.IgnoringLeadingWhiteSpace()`  |
| `o => o.IgnoringTrailingWhitespace()` | `.IgnoringTrailingWhiteSpace()` |
| `o => o.IgnoringNewlineStyle()`       | `.IgnoringNewlineStyle()`       |
| `o => o.WithoutStrictOrdering()`      | `.InAnyOrder()`                 |

Options that are not such a lambda, or any other option, such as `Using<T>(...)`, would be lost in the rewrite, so no code fix is offered.

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
string[] genres = // ...
Exception[] errors = // ...

// highlight-start
genres.Should().AllBeEquivalentTo("rock", o => o.IgnoringCase());
errors.Should().AllBeOfType<ArgumentException>();
// highlight-end
```

```csharp title="After (aweXpect)"
string[] genres = // ...
Exception[] errors = // ...

// highlight-start
Expect.That(genres).All().AreEqualTo("rock").IgnoringCase();
Expect.That(errors).All().AreExactly<ArgumentException>();
// highlight-end
```

</ApplyFix>

## Object equivalency

| FluentAssertions construct                                                    | Rewritten to                                                                           | Note                                              |
|-------------------------------------------------------------------------------|----------------------------------------------------------------------------------------|---------------------------------------------------|
| `subject.Should().BeEquivalentTo(expected)` *(arbitrary object)*              | `Expect.That(subject).IsEquivalentTo(expected, o => o.IgnoringCollectionOrder())`      | [structural equivalency](./02-fluentassertions-differences.md#structural-equivalency) |
| `subject.Should().NotBeEquivalentTo(unexpected)` *(arbitrary object)*         | `Expect.That(subject).IsNotEquivalentTo(unexpected, o => o.IgnoringCollectionOrder())` | [structural equivalency](./02-fluentassertions-differences.md#structural-equivalency) |
| `subject.Should().BeEquivalentTo(expected)` *(object compared by value)*      | `Expect.That(subject).IsEqualTo(expected)`                                             |                                                   |
| `subject.Should().NotBeEquivalentTo(unexpected)` *(object compared by value)* | `Expect.That(subject).IsNotEqualTo(unexpected)`                                        |                                                   |

## Exceptions

| FluentAssertions construct                                       | Rewritten to                                                               | Note                                                       |
|------------------------------------------------------------------|----------------------------------------------------------------------------|------------------------------------------------------------|
| `callback.Should().NotThrow()` / `NotThrowAsync()`               | `Expect.That(callback).DoesNotThrow()`                                     |                                                            |
| `callback.Should().NotThrow<T>()` / `NotThrowAsync<T>()`         | `Expect.That(callback).DoesNotThrow<T>()`                                  | [AggregateException](./02-fluentassertions-differences.md#aggregateexception-is-not-unwrapped) |
| `callback.Should().Throw<T>()` / `ThrowAsync<T>()`               | `Expect.That(callback).Throws<T>()`                                        | [AggregateException](./02-fluentassertions-differences.md#aggregateexception-is-not-unwrapped) |
| `callback.Should().ThrowExactly<T>()` / `ThrowExactlyAsync<T>()` | `Expect.That(callback).ThrowsExactly<T>()`                                 |                                                            |
| `callback.Should().ThrowWithinAsync<T>(timeout)`                 | `Expect.That(callback).Throws<T>().Within(timeout)`                        | [AggregateException](./02-fluentassertions-differences.md#aggregateexception-is-not-unwrapped) |
| `callback.Should().CompleteWithinAsync(timeout)`                 | `Expect.That(callback).ExecutesIn().AtMost(timeout)`                       | [canceled tasks](./02-fluentassertions-differences.md#canceled-tasks-do-not-complete)          |
| `.WithMessage(pattern)`                                          | `.WithMessage(pattern).AsWildcard().IgnoringCase().IgnoringNewlineStyle()` | [empty patterns](./02-fluentassertions-differences.md#empty-wildcard-patterns)                 |
| `.WithInnerException<T>()` / `(typeof(T))`                       | `.WithInner<T>()` / `.WithInner(typeof(T))`                                |                                                            |
| `.WithParameterName(name)`                                       | `.WithParamName(name)`                                                     |                                                            |

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
Action addTrack = // ...

// highlight-next-line
addTrack.Should().Throw<ArgumentException>().WithMessage("*track number*");
```

```csharp title="After (aweXpect)"
Action addTrack = // ...

// highlight-next-line
Expect.That(addTrack).Throws<ArgumentException>().WithMessage("*track number*").AsWildcard().IgnoringCase().IgnoringNewlineStyle();
```

</ApplyFix>

A chained call without an aweXpect counterpart, such as `Where(...)` or `WithInnerExceptionExactly<T>()`, leaves the assertion for manual migration. So does a call chained after `WithInnerException`, because FluentAssertions continues on the inner exception there, whereas `WithInner` returns to the outer one.

## Dates and times

| FluentAssertions construct                                                            | Rewritten to                                                            | Note                             |
|---------------------------------------------------------------------------------------|-------------------------------------------------------------------------|----------------------------------|
| `subject.Should().BeAfter(expected)`                                                  | `Expect.That(subject).IsAfter(expected)`                                | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds) |
| `subject.Should().BeOnOrAfter(expected)`                                              | `Expect.That(subject).IsOnOrAfter(expected)`                            | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds) |
| `subject.Should().BeBefore(expected)`                                                 | `Expect.That(subject).IsBefore(expected)`                               | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds) |
| `subject.Should().BeOnOrBefore(expected)`                                             | `Expect.That(subject).IsOnOrBefore(expected)`                           | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds) |
| `subject.Should().NotBeAfter(unexpected)` / `NotBeOnOrAfter(...)`                     | `Expect.That(subject).IsNotAfter(unexpected)` / `IsNotOnOrAfter(...)`   | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds) |
| `subject.Should().NotBeBefore(unexpected)` / `NotBeOnOrBefore(...)`                   | `Expect.That(subject).IsNotBefore(unexpected)` / `IsNotOnOrBefore(...)` | [DateTime kind](./02-fluentassertions-differences.md#datetime-kinds) |
| `subject.Should().HaveYear(n)` (also `Month`/`Day`/`Hour`/`Minute`/`Second`/`Offset`) | `Expect.That(subject).HasYear().EqualTo(n)` (etc.)                      |                                  |
| `subject.Should().NotHaveYear(n)` (and the other parts)                               | `Expect.That(subject).HasYear().NotEqualTo(n)` (etc.)                   |                                  |

`Be`, `NotBe`, `BeCloseTo`, `NotBeCloseTo` and `BeOneOf` on dates are listed under [Equality](#equality); the [kind of a `DateTime`](./02-fluentassertions-differences.md#datetime-kinds) matters there as well.

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
DateTime releaseDate = // ...

// highlight-start
releaseDate.Should().HaveYear(1969);
releaseDate.Should().NotHaveMonth(1);
// highlight-end
```

```csharp title="After (aweXpect)"
DateTime releaseDate = // ...

// highlight-start
Expect.That(releaseDate).HasYear().EqualTo(1969);
Expect.That(releaseDate).HasMonth().NotEqualTo(1);
// highlight-end
```

</ApplyFix>

## Enums and nullable values

| FluentAssertions construct                     | Rewritten to                                     | Note                                                 |
|------------------------------------------------|--------------------------------------------------|------------------------------------------------------|
| `subject.Should().BeDefined()`                 | `Expect.That(subject).IsDefined()`               |                                                      |
| `subject.Should().NotBeDefined()`              | `Expect.That(subject).IsNotDefined()`            |                                                      |
| `subject.Should().HaveFlag(flag)`              | `Expect.That(subject).HasFlag(flag)`             |                                                      |
| `subject.Should().NotHaveFlag(flag)`           | `Expect.That(subject).DoesNotHaveFlag(flag)`     | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null)  |
| `subject.Should().HaveValue()` (`Nullable<T>`) | `Expect.That(subject).IsNotNull()`               |                                                      |
| `subject.Should().HaveValue(v)`                | `Expect.That(subject).HasValue(v)`               |                                                      |
| `subject.Should().NotHaveValue()`              | `Expect.That(subject).IsNull()`                  |                                                      |
| `subject.Should().NotHaveValue(v)`             | `Expect.That(subject).HasValue().NotEqualTo(v)`  | [null subjects](./02-fluentassertions-differences.md#negated-inspections-fail-for-null)  |

## Lambda assertions

When the entire `.Should()` chain is the body of a lambda (so it is not awaited at the call site and the surrounding signature is `Action` or `Func<T>`), the code fix wraps the rewritten expression in `aweXpect.Synchronous.Synchronously.Verify(...)` to keep the lambda synchronous:

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
bool isPlaying = // ...

Action verify = () => isPlaying.Should().BeTrue();
```

```csharp no-compile title="After (aweXpect)"
bool isPlaying = // ...

Action verify = () => aweXpect.Synchronous.Synchronously.Verify(Expect.That(isPlaying).IsTrue());
```

</ApplyFix>

## Because messages

A trailing `because` argument on any of the assertions above is preserved as a `.Because(...)` suffix, including formatted overloads:

<ApplyFix id="aweXpectM002" migrateTo="aweXpect">

```csharp title="Before (FluentAssertions)"
int trackCount = // ...
string album = // ...

// highlight-next-line
trackCount.Should().Be(17, "{0} has 17 tracks", album);
```

```csharp title="After (aweXpect)"
int trackCount = // ...
string album = // ...

// highlight-next-line
Expect.That(trackCount).IsEqualTo(17).Because($"{album} has 17 tracks");
```

</ApplyFix>

## Behavioral differences

A few rewrites pass or fail for different inputs than the original FluentAssertions assertion, by a deliberate choice of aweXpect. Each case is listed with an example and a way to restore the original behavior on [Behavioral differences from FluentAssertions](./02-fluentassertions-differences.md).
