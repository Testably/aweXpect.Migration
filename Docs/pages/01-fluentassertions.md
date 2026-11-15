---
description: The FluentAssertions assertions that aweXpect.Migration rewrites to aweXpect, and the cases in which the rewrite behaves differently.
---

import ApplyFix from '@site/src/components/ApplyFix';

# FluentAssertions

The code fix rewrites the entire `.Should()` chain in one step: the `Should()` call is dropped, `Expect.That(subject)` becomes the new root, the assertion methods are renamed, and any trailing `because` argument is moved into a `.Because(...)` suffix. `.And` chains are preserved.

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

The examples show the code as the code fix writes it; the `await` is added in a [second step](./00-index.md#how-it-works).

The tables below are grouped by **target concept** (equality, containment and so on), not by the FluentAssertions documentation pages. Most of the basic assertions (`Be`, `NotBe`, `BeEmpty`, `Contain`, `StartWith` and others) are not specific to one subject type; they apply to any compatible subject.

A chain that continues on a second `.Should()`, e.g. after `.Which`, is left for manual migration, as is any call the code fix cannot translate without losing an argument.

A note in the last column marks a rewrite that behaves differently in some cases; it links to the matching entry under [Behavioral differences](#behavioral-differences).

## Equality

| FluentAssertions construct                                  | Rewritten to                                                    | Note                                                                                                         |
|-------------------------------------------------------------|-----------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| `subject.Should().Be(expected)`                             | `Expect.That(subject).IsEqualTo(expected)`                      | [DateTime kind](#datetime-kinds)                                                                             |
| `subject.Should().NotBe(unexpected)`                        | `Expect.That(subject).IsNotEqualTo(unexpected)`                 | [DateTime kind](#datetime-kinds)                                                                             |
| `subject.Should().BeApproximately(expected, tolerance)`     | `Expect.That(subject).IsEqualTo(expected).Within(tolerance)`    | [NaN](#nan-is-an-ordinary-value)                                                                             |
| `subject.Should().NotBeApproximately(expected, tolerance)`  | `Expect.That(subject).IsNotEqualTo(expected).Within(tolerance)` | [NaN](#nan-is-an-ordinary-value)                                                                             |
| `subject.Should().BeCloseTo(expected, delta)`               | `Expect.That(subject).IsEqualTo(expected).Within(delta)`        | [integral deltas](#integral-tolerances), [DateTime kind](#datetime-kinds)                                    |
| `subject.Should().NotBeCloseTo(expected, delta)`            | `Expect.That(subject).IsNotEqualTo(expected).Within(delta)`     | [integral deltas](#integral-tolerances), [DateTime kind](#datetime-kinds)                                    |
| `subject.Should().BeOneOf(a, b, c)`                         | `Expect.That(subject).IsOneOf(a, b, c)`                         | [mixed number types](#numbers-of-different-types), [DateTime kind](#datetime-kinds)                          |
| `subject.Should().BeOneOf(collection)`                      | `Expect.That(subject).IsOneOf(collection)`                      | [mixed number types](#numbers-of-different-types), [empty expectations](#empty-expectations-throw), [DateTime kind](#datetime-kinds) |
| `subject.Should().BeSameAs(expected)`                       | `Expect.That(subject).IsSameAs(expected)`                       |                                                                                                              |
| `subject.Should().NotBeSameAs(unexpected)`                  | `Expect.That(subject).IsNotSameAs(unexpected)`                  |                                                                                                              |

For an integral subject, FluentAssertions takes the delta of `BeCloseTo` as the unsigned counterpart of the subject type (`uint` for `int`). When the delta does not convert implicitly, the code fix casts it to the subject type, e.g. `Within((int)delta)`. A constant delta that does not fit the subject type, such as `uint.MaxValue` for an `int`, is left for manual migration.

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

For string subjects the `o => o...` options are translated as well. As in FluentAssertions, a string compared with options ignores case only when the options contain `IgnoringCase()`:

| FluentAssertions option               | Appended to the rewrite         |
|---------------------------------------|---------------------------------|
| `o => o.IgnoringCase()`               | `.IgnoringCase()`               |
| `o => o.IgnoringLeadingWhitespace()`  | `.IgnoringLeadingWhiteSpace()`  |
| `o => o.IgnoringTrailingWhitespace()` | `.IgnoringTrailingWhiteSpace()` |
| `o => o.IgnoringNewlineStyle()`       | `.IgnoringNewlineStyle()`       |

For structural comparisons, options other than `WithStrictOrdering()` and `WithoutStrictOrdering()` have no automatic translation, so no code fix is offered. `NotBeEquivalentTo` is translated symmetrically (`IsNotEqualTo` / `IsNotEquivalentTo`). See also [Structural equivalency](#structural-equivalency) under the behavioral differences.

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
| `subject.Should().NotBeEmpty()`                         | `Expect.That(subject).IsNotEmpty()`              | [null subjects](#negated-inspections-fail-for-null)  |
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

On a `System.Type` subject, `BeAssignableTo` and `NotBeAssignableTo` are not rewritten; see [Assertions on a `Type`](#assertions-on-a-type).

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
| `subject.Should().BeInRange(low, high)`                                       | `Expect.That(subject).IsBetween(low).And(high)`         | [reversed ranges](#reversed-ranges-throw)   |
| `subject.Should().NotBeInRange(low, high)`                                    | `Expect.That(subject).IsNotBetween(low).And(high)`      | [reversed ranges](#reversed-ranges-throw)   |

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
| `subject.Should().NotContain(unexpected)`                               | `Expect.That(subject).DoesNotContain(unexpected)`                                                         | [null subjects](#negated-inspections-fail-for-null)  |
| `subject.Should().NotContain(collection)`                               | `Expect.That(subject).None().ComplyWith(x => x.IsOneOf(collection))`                                      | [mixed number types](#numbers-of-different-types)    |
| `subject.Should().StartWith(expected)`                                  | `Expect.That(subject).StartsWith(expected)`                                                               | [empty expectations](#empty-expectations-throw)      |
| `subject.Should().NotStartWith(unexpected)`                             | `Expect.That(subject).DoesNotStartWith(unexpected)`                                                       | [empty expectations](#empty-expectations-throw)      |
| `subject.Should().EndWith(expected)`                                    | `Expect.That(subject).EndsWith(expected)`                                                                 | [empty expectations](#empty-expectations-throw)      |
| `subject.Should().NotEndWith(unexpected)`                               | `Expect.That(subject).DoesNotEndWith(unexpected)`                                                         | [empty expectations](#empty-expectations-throw)      |
| `subject.Should().ContainInOrder(collection)`                           | `Expect.That(subject).Contains(collection).IgnoringInterspersedItems()`                                   | [empty expectations](#empty-expectations-throw)      |
| `subject.Should().ContainInOrder(a, b, c)`                              | `Expect.That(subject).Contains([a, b, c]).IgnoringInterspersedItems()`                                    |                                                      |
| `subject.Should().ContainInConsecutiveOrder(collection)`                | `Expect.That(subject).Contains(collection)`                                                               | [empty expectations](#empty-expectations-throw)      |
| `subject.Should().ContainInConsecutiveOrder(a, b, c)`                   | `Expect.That(subject).Contains([a, b, c])`                                                                |                                                      |
| `subject.Should().ContainEquivalentOf(expected)`                        | `Expect.That(subject).Contains(expected).Equivalent(o => o.IgnoringCollectionOrder())`                    | [structural equivalency](#structural-equivalency)    |
| `subject.Should().NotContainEquivalentOf(unexpected)`                   | `Expect.That(subject).DoesNotContain(unexpected).Equivalent(o => o.IgnoringCollectionOrder())`            | [structural equivalency](#structural-equivalency)    |
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
| `subject.Should().Match(pattern)` *(string subject)*                    | `Expect.That(subject).IsEqualTo(pattern).AsWildcard()`                                                    | [empty patterns](#empty-wildcard-patterns)           |

For items that are compared by value (see [Equivalency](#equivalency)), `ContainEquivalentOf` and `NotContainEquivalentOf` are rewritten to `Contains(expected)` and `DoesNotContain(unexpected)` without `Equivalent()`. With `o => o.WithStrictOrdering()` they use `Equivalent()` without options.

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

`Should().Contain(expected, occurrence)` translates the full FluentAssertions occurrence family. The `Thrice` and `Times(n)` cases are emitted as numeric arguments, not as a chained `.Thrice()`. Occurrences are [counted without overlap](#occurrences-are-counted-without-overlap).

| FluentAssertions occurrence | Appended to the rewrite | Note                                                |
|-----------------------------|-------------------------|-----------------------------------------------------|
| `AtLeast.Once()`            | `.AtLeast().Once()`     |                                                     |
| `AtLeast.Twice()`           | `.AtLeast().Twice()`    |                                                     |
| `AtLeast.Thrice()`          | `.AtLeast(3)`           |                                                     |
| `AtLeast.Times(n)`          | `.AtLeast(n)`           |                                                     |
| `AtMost.Once()`             | `.AtMost().Once()`      | [null subjects](#negated-inspections-fail-for-null) |
| `AtMost.Twice()`            | `.AtMost().Twice()`     | [null subjects](#negated-inspections-fail-for-null) |
| `AtMost.Thrice()`           | `.AtMost(3)`            | [null subjects](#negated-inspections-fail-for-null) |
| `AtMost.Times(n)`           | `.AtMost(n)`            | [null subjects](#negated-inspections-fail-for-null) |
| `Exactly.Once()`            | `.Once()`               |                                                     |
| `Exactly.Twice()`           | `.Twice()`              |                                                     |
| `Exactly.Thrice()`          | `.Exactly(3)`           |                                                     |
| `Exactly.Times(n)`          | `.Exactly(n)`           |                                                     |
| `LessThan.Twice()`          | `.LessThan().Twice()`   | [null subjects](#negated-inspections-fail-for-null) |
| `LessThan.Thrice()`         | `.LessThan(3)`          | [null subjects](#negated-inspections-fail-for-null) |
| `LessThan.Times(n)`         | `.LessThan(n)`          | [null subjects](#negated-inspections-fail-for-null) |
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
| `subject.Should().AllBeAssignableTo<T>()` / `(typeof(T))`                | `Expect.That(subject).All().Are<T>()` / `All().Are(typeof(T))`                           | [Type items](#type-instances-as-items)            |
| `subject.Should().AllBeOfType<T>()` / `(typeof(T))`                      | `Expect.That(subject).All().AreExactly<T>()` / `All().AreExactly(typeof(T))`             | [Type items](#type-instances-as-items)            |
| `subject.Should().AllBeEquivalentTo(expected)` *(non-string elements)*   | `Expect.That(subject).All().AreEquivalentTo(expected, o => o.IgnoringCollectionOrder())` | [structural equivalency](#structural-equivalency) |
| `subject.Should().AllBeEquivalentTo(expected)` *(`IEnumerable<string>`)* | `Expect.That(subject).All().AreEqualTo(expected)`                                        |                                                   |

For elements that are compared by value (see [Equivalency](#equivalency)), `AllBeEquivalentTo` is rewritten to `All().AreEqualTo(expected)`; with `o => o.WithStrictOrdering()` it is rewritten to `All().AreEquivalentTo(expected)` without the options lambda. For `IEnumerable<string>` collections, the `o => o...` options are translated as well:

| FluentAssertions option               | Appended to the rewrite         |
|---------------------------------------|---------------------------------|
| `o => o.IgnoringCase()`               | `.IgnoringCase()`               |
| `o => o.IgnoringLeadingWhitespace()`  | `.IgnoringLeadingWhiteSpace()`  |
| `o => o.IgnoringTrailingWhitespace()` | `.IgnoringTrailingWhiteSpace()` |
| `o => o.IgnoringNewlineStyle()`       | `.IgnoringNewlineStyle()`       |
| `o => o.WithoutStrictOrdering()`      | `.InAnyOrder()`                 |

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
| `subject.Should().BeEquivalentTo(expected)` *(arbitrary object)*              | `Expect.That(subject).IsEquivalentTo(expected, o => o.IgnoringCollectionOrder())`      | [structural equivalency](#structural-equivalency) |
| `subject.Should().NotBeEquivalentTo(unexpected)` *(arbitrary object)*         | `Expect.That(subject).IsNotEquivalentTo(unexpected, o => o.IgnoringCollectionOrder())` | [structural equivalency](#structural-equivalency) |
| `subject.Should().BeEquivalentTo(expected)` *(object compared by value)*      | `Expect.That(subject).IsEqualTo(expected)`                                             |                                                   |
| `subject.Should().NotBeEquivalentTo(unexpected)` *(object compared by value)* | `Expect.That(subject).IsNotEqualTo(unexpected)`                                        |                                                   |

## Exceptions

| FluentAssertions construct                                       | Rewritten to                                                               | Note                                                       |
|------------------------------------------------------------------|----------------------------------------------------------------------------|------------------------------------------------------------|
| `callback.Should().NotThrow()` / `NotThrowAsync()`               | `Expect.That(callback).DoesNotThrow()`                                     |                                                            |
| `callback.Should().NotThrow<T>()` / `NotThrowAsync<T>()`         | `Expect.That(callback).DoesNotThrow<T>()`                                  | [AggregateException](#aggregateexception-is-not-unwrapped) |
| `callback.Should().Throw<T>()` / `ThrowAsync<T>()`               | `Expect.That(callback).Throws<T>()`                                        | [AggregateException](#aggregateexception-is-not-unwrapped) |
| `callback.Should().ThrowExactly<T>()` / `ThrowExactlyAsync<T>()` | `Expect.That(callback).ThrowsExactly<T>()`                                 |                                                            |
| `callback.Should().ThrowWithinAsync<T>(timeout)`                 | `Expect.That(callback).Throws<T>().Within(timeout)`                        | [AggregateException](#aggregateexception-is-not-unwrapped) |
| `callback.Should().CompleteWithinAsync(timeout)`                 | `Expect.That(callback).ExecutesIn().AtMost(timeout)`                       | [canceled tasks](#canceled-tasks-do-not-complete)          |
| `.WithMessage(pattern)`                                          | `.WithMessage(pattern).AsWildcard().IgnoringCase().IgnoringNewlineStyle()` | [empty patterns](#empty-wildcard-patterns)                 |
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
| `subject.Should().BeAfter(expected)`                                                  | `Expect.That(subject).IsAfter(expected)`                                | [DateTime kind](#datetime-kinds) |
| `subject.Should().BeOnOrAfter(expected)`                                              | `Expect.That(subject).IsOnOrAfter(expected)`                            | [DateTime kind](#datetime-kinds) |
| `subject.Should().BeBefore(expected)`                                                 | `Expect.That(subject).IsBefore(expected)`                               | [DateTime kind](#datetime-kinds) |
| `subject.Should().BeOnOrBefore(expected)`                                             | `Expect.That(subject).IsOnOrBefore(expected)`                           | [DateTime kind](#datetime-kinds) |
| `subject.Should().NotBeAfter(unexpected)` / `NotBeOnOrAfter(...)`                     | `Expect.That(subject).IsNotAfter(unexpected)` / `IsNotOnOrAfter(...)`   | [DateTime kind](#datetime-kinds) |
| `subject.Should().NotBeBefore(unexpected)` / `NotBeOnOrBefore(...)`                   | `Expect.That(subject).IsNotBefore(unexpected)` / `IsNotOnOrBefore(...)` | [DateTime kind](#datetime-kinds) |
| `subject.Should().HaveYear(n)` (also `Month`/`Day`/`Hour`/`Minute`/`Second`/`Offset`) | `Expect.That(subject).HasYear().EqualTo(n)` (etc.)                      |                                  |
| `subject.Should().NotHaveYear(n)` (and the other parts)                               | `Expect.That(subject).HasYear().NotEqualTo(n)` (etc.)                   |                                  |

`Be`, `NotBe`, `BeCloseTo`, `NotBeCloseTo` and `BeOneOf` on dates are listed under [Equality](#equality); the [kind of a `DateTime`](#datetime-kinds) matters there as well.

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
| `subject.Should().NotHaveFlag(flag)`           | `Expect.That(subject).DoesNotHaveFlag(flag)`     | [null subjects](#negated-inspections-fail-for-null)  |
| `subject.Should().HaveValue()` (`Nullable<T>`) | `Expect.That(subject).IsNotNull()`               |                                                      |
| `subject.Should().HaveValue(v)`                | `Expect.That(subject).HasValue(v)`               |                                                      |
| `subject.Should().NotHaveValue()`              | `Expect.That(subject).IsNull()`                  |                                                      |
| `subject.Should().NotHaveValue(v)`             | `Expect.That(subject).HasValue().NotEqualTo(v)`  | [null subjects](#negated-inspections-fail-for-null)  |

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

Compared with FluentAssertions 8, a few rewritten assertions pass or fail for different inputs than the original. These are choices aweXpect makes on purpose, and the code fix cannot bridge them. Most follow from a few rules:

- **Strict by default, loosen explicitly.** The default is the strictest reasonable reading, and each relaxation is an opt-in with a name, such as `AsWildcard()`, `IgnoringCase()`, `InAnyOrder()` or `IgnoringCollectionOrder()`.
- **A negation is the exact complement.** `DoesNotX` passes exactly when `X` fails.
- **`null` never passes an inspection by accident.** Equality treats `null` as a value; expectations that inspect the content fail for `null` in both polarities.
- **Impossible or malformed expectations are programming errors.** They throw an argument exception instead of failing or passing.
- **No silent guessing.** A comparison that cannot be answered honestly fails in both polarities.
- **What you pass is what is compared.** The runtime type drives equivalency, `Equals` overrides are not trusted, numbers and enums of different types are not converted in equivalency, and an `AggregateException` is not unwrapped.

Each example below runs the original assertion and its rewrite on the same input and shows the outcome of both.

### Equality

#### NaN is an ordinary value

`BeApproximately(double.NaN, tolerance)` throws in FluentAssertions and `NotBeApproximately` fails for a `NaN` subject. The rewrites pass: aweXpect compares `NaN` like `double.Equals` does, as a value that equals itself and nothing else.

```csharp
double subject = double.NaN;

// FluentAssertions: throws an ArgumentException
subject.Should().BeApproximately(double.NaN, 0.1);
// aweXpect: passes
await That(subject).IsEqualTo(double.NaN).Within(0.1);

// FluentAssertions: fails
subject.Should().NotBeApproximately(1.0, 0.1);
// aweXpect: passes
await That(subject).IsNotEqualTo(1.0).Within(0.1);
```

Add `IsNotNaN()` where a `NaN` subject has to fail:

```csharp
double subject = // ...
double expected = // ...
double tolerance = // ...

await That(subject).IsNotNaN().And.IsNotEqualTo(expected).Within(tolerance);
```

#### Integral tolerances

The rewrite casts an unsigned `BeCloseTo` delta to the subject type. A variable delta that does not fit, such as `uint.MaxValue` for an `int`, becomes negative and throws an `ArgumentOutOfRangeException`, because a negative tolerance is a malformed expectation.

```csharp
int subject = 1;
uint delta = uint.MaxValue;

// FluentAssertions: passes
subject.Should().BeCloseTo(1, delta);
// aweXpect: throws an ArgumentOutOfRangeException, as (int)delta is -1
await That(subject).IsEqualTo(1).Within((int)delta);
```

#### Numbers of different types

`IsOneOf` on an `object` subject compares numbers by value, like `IsEqualTo`, so `That((object)1).IsOneOf(1L, 2L)` passes where `BeOneOf` fails. The rewrite of `NotContain(collection)` uses `IsOneOf` as well and therefore also rejects an item `1` for an unexpected `1L`. aweXpect applies one rule to all equality expectations on `object`.

```csharp
object subject = 1;

// FluentAssertions: fails
subject.Should().BeOneOf(1L, 2L);
// aweXpect: passes
await That(subject).IsOneOf(1L, 2L);

object[] items = [1];
object[] unexpected = [1L];

// FluentAssertions: passes
items.Should().NotContain(unexpected);
// aweXpect: fails
await That(items).None().ComplyWith(x => x.IsOneOf(unexpected));
```

### Null and emptiness

#### Negated inspections fail for null

`NotBeEmpty()` and `NotContain("Yesterday")` pass in FluentAssertions for a `null` string, and `NotHaveValue(2)` and `NotHaveFlag(flag)` pass for a `null` enum. The same holds for `Contain("Yesterday", AtMost.Once())`, `Contain("Yesterday", LessThan.Twice())` and any other occurrence constraint that allows zero occurrences. Their rewrites fail, because an expectation that inspects the content has nothing to inspect in `null`.

```csharp
string? subject = null;

// FluentAssertions: passes
subject.Should().NotBeEmpty();
// aweXpect: fails
await That(subject).IsNotEmpty();

// FluentAssertions: passes
subject.Should().NotContain("Yesterday");
// aweXpect: fails
await That(subject).DoesNotContain("Yesterday");

// FluentAssertions: passes
subject.Should().Contain("Yesterday", AtMost.Once());
// aweXpect: fails
await That(subject).Contains("Yesterday").AtMost().Once();
```

Allow `null` explicitly where it is acceptable:

```csharp
string? subject = // ...

await That(subject).IsNull().Or.IsNotEmpty();
await That(subject).IsNull().Or.Contains("Yesterday").AtMost().Once();
```

### Type checks

#### Assertions on a `Type`

FluentAssertions checks the type that a `System.Type` subject describes, so `typeof(string).Should().BeAssignableTo<IComparable>()` passes. aweXpect has no expectations on a `Type` subject: `Is<IComparable>()` would check the `Type` object itself and silently invert `NotBeAssignableTo`.

```csharp
Type subject = typeof(string);

// FluentAssertions: passes, as string implements IComparable
subject.Should().BeAssignableTo<IComparable>();
// aweXpect: fails, as the Type object itself does not implement IComparable
await That(subject).Is<IComparable>();
```

No code fix is offered; migrate such assertions by hand:

```csharp
Type subject = // ...

await That(typeof(IComparable).IsAssignableFrom(subject)).IsTrue();
```

### Numbers

#### Reversed ranges throw

`NotBeInRange(3, 1)` passes in FluentAssertions for every value, and `BeInRange(3, 1)` fails for every value. `IsNotBetween(3).And(1)` and `IsBetween(3).And(1)` throw an `ArgumentOutOfRangeException`, because a range whose maximum is below its minimum is a malformed expectation. Swap the bounds.

```csharp
int subject = 2;

// FluentAssertions: passes
subject.Should().NotBeInRange(3, 1);
// aweXpect: throws an ArgumentOutOfRangeException
await That(subject).IsNotBetween(3).And(1);
```

### Containment

#### Empty expectations throw

`StartWith("")`, `EndWith("")`, and `StartWith`, `ContainInOrder` or `ContainInConsecutiveOrder` with an empty collection pass in FluentAssertions for every subject. `NotStartWith("")`, `NotEndWith("")`, and `BeOneOf` with an empty collection fail for every subject. The rewrites throw an `ArgumentException`, because an expectation that cannot fail checks nothing, and one that cannot pass is a mistake. Remove the assertion or expect something concrete.

```csharp
string subject = "Abbey Road";

// FluentAssertions: passes
subject.Should().StartWith("");
// aweXpect: throws an ArgumentException
await That(subject).StartsWith("");

// FluentAssertions: fails
subject.Should().NotStartWith("");
// aweXpect: throws an ArgumentException
await That(subject).DoesNotStartWith("");
```

#### Empty wildcard patterns

`Match("")` on a string and `WithMessage("")` on an exception throw an `ArgumentException` in FluentAssertions. The rewrites `IsEqualTo("").AsWildcard()` and `WithMessage("").AsWildcard()` do not throw: an empty pattern matches only the empty string, so it passes for `""` and fails for any other subject or message.

```csharp
string subject = "";

// FluentAssertions: throws an ArgumentException
subject.Should().Match("");
// aweXpect: passes
await That(subject).IsEqualTo("").AsWildcard();

Action callback = () => throw new Exception("");

// FluentAssertions: throws an ArgumentException
callback.Should().Throw<Exception>().WithMessage("");
// aweXpect: passes
await That(callback).Throws<Exception>().WithMessage("").AsWildcard().IgnoringCase().IgnoringNewlineStyle();
```

#### Occurrences are counted without overlap

`Contains(x)` with an occurrence constraint counts non-overlapping occurrences, the way `Regex.Matches` and a loop over `IndexOf` do, so `"nananana"` contains `"nana"` exactly twice. FluentAssertions counts every position at which the substring starts, which is three times.

```csharp
string subject = "nananana";

// FluentAssertions: fails, as it counts "nana" three times
subject.Should().Contain("nana", Exactly.Twice());
// aweXpect: passes
await That(subject).Contains("nana").Twice();
```

### Collections

#### Type instances as items

`AllBeAssignableTo<T>()` and `AllBeOfType<T>()` also accept `Type` instances that describe a matching type. `All().Are<T>()` and `All().AreExactly<T>()` check the runtime type of each item, and the runtime type of a `Type` object is `RuntimeType`.

```csharp
Type[] subject = [typeof(ArgumentException)];

// FluentAssertions: passes
subject.Should().AllBeAssignableTo<Exception>();
// aweXpect: fails, as the item is a Type and not an Exception
await That(subject).All().Are<Exception>();
```

Check the described types explicitly:

```csharp
Type[] subject = // ...

await That(subject).All().Satisfy(type => typeof(Exception).IsAssignableFrom(type));
```

### Structural equivalency

The rewrites add `IgnoringCollectionOrder()` to match the FluentAssertions default for nested collections. The following differences remain:

- **The runtime type of the expectation drives the comparison.** aweXpect compares all public members of the expectation's runtime type; FluentAssertions only those of its declared type. Ignore a member that should not count with `o => o.IgnoringMember("Venue")`.
  ```csharp
  using aweXpect.Equivalency;

  class Track { public string Title { get; set; } = ""; }
  class LiveTrack : Track { public string Venue { get; set; } = ""; }

  Track subject = new LiveTrack { Title = "Hey Jude", Venue = "Wembley" };
  Track expected = new LiveTrack { Title = "Hey Jude", Venue = "Shea Stadium" };

  // FluentAssertions: passes, as Track has no Venue
  subject.Should().BeEquivalentTo(expected);
  // aweXpect: fails, as LiveTrack has a different Venue
  await That(subject).IsEquivalentTo(expected, o => o.IgnoringCollectionOrder());
  ```
- **`Equals` overrides of nested objects are ignored.** When the type of the expectation itself overrides `Equals`, the rewrite uses `IsEqualTo` (see [Equivalency](#equivalency)). A nested object whose type overrides `Equals` is compared differently: FluentAssertions compares it with `Equals`; aweXpect compares its members, so an `Equals` that ignores a member no longer hides a difference in it.
  ```csharp
  // Rating.Equals compares only the Stars
  class Rating
  {
      public int Stars { get; set; }
      public string Comment { get; set; } = "";
      public override bool Equals(object? obj) => obj is Rating other && other.Stars == Stars;
      public override int GetHashCode() => Stars.GetHashCode();
  }
  class Review { public Rating Rating { get; set; } = new(); }

  Review subject = new Review { Rating = new Rating { Stars = 5, Comment = "timeless" } };
  Review expected = new Review { Rating = new Rating { Stars = 5, Comment = "overrated" } };

  // FluentAssertions: passes
  subject.Should().BeEquivalentTo(expected);
  // aweXpect: fails, as the Comment differs
  await That(subject).IsEquivalentTo(expected, o => o.IgnoringCollectionOrder());
  ```
  Ask for the type's own equality explicitly:
  ```csharp
  Review subject = // ...
  Review expected = // ...

  await That(subject).IsEquivalentTo(expected, o => o
      .IgnoringCollectionOrder()
      .For<Rating>(x => x with { ComparisonType = EquivalencyComparisonType.ByValue }));
  ```
- **Numbers of different types are not converted.** A member of type `int` is not equivalent to a member of type `long` with the same value. Use the same type in the expectation, e.g. `1` instead of `1L`.
  ```csharp
  class ChartEntry { public int Position { get; set; } }

  ChartEntry subject = new ChartEntry { Position = 1 };

  // FluentAssertions: passes
  subject.Should().BeEquivalentTo(new { Position = 1L });
  // aweXpect: fails, as Position is an int and 1L is a long
  await That(subject).IsEquivalentTo(new { Position = 1L }, o => o.IgnoringCollectionOrder());
  ```
- **Enums of different types are not equivalent.** FluentAssertions compares enum members by their value, also across enum types; aweXpect compares them with `Equals`, which requires the same type. Compare such a pair of types with a predicate.
  ```csharp
  enum Genre { Pop, Rock }
  enum GenreDto { Pop, Rock }
  class Album { public Genre Genre { get; set; } }

  Album subject = new Album { Genre = Genre.Rock };

  // FluentAssertions: passes
  subject.Should().BeEquivalentTo(new { Genre = GenreDto.Rock });
  // aweXpect: fails, as Genre and GenreDto are different types
  await That(subject).IsEquivalentTo(new { Genre = GenreDto.Rock }, o => o.IgnoringCollectionOrder());
  ```
  ```csharp
  Album subject = // ...

  await That(subject).IsEquivalentTo(new { Genre = GenreDto.Rock }, o => o
      .IgnoringCollectionOrder()
      .Comparing<Genre, GenreDto>((genre, dto) => (int)genre == (int)dto));
  ```
- **Cyclic references and deep graphs are compared.** FluentAssertions fails for a cyclic reference without `IgnoringCyclicReferences()` and for a graph nested deeper than 10 levels. aweXpect detects cycles and fails only at a depth of 100 nested objects, so the rewrite passes for such graphs when they are equivalent.

### Exceptions

#### AggregateException is not unwrapped

FluentAssertions looks through an `AggregateException`, so `Throw<ArgumentException>()` passes for an aggregate that wraps an `ArgumentException`, and `NotThrow<ArgumentException>()` fails for it. aweXpect compares the exception the delegate actually threw, so `Throws<ArgumentException>()` fails and `DoesNotThrow<ArgumentException>()` passes. For the same reason, `Throws<Exception>()` passes for an `AggregateException` without inner exceptions, for which `Throw<Exception>()` fails in FluentAssertions.

```csharp
Action callback = () => throw new AggregateException(new ArgumentException());

// FluentAssertions: passes
callback.Should().Throw<ArgumentException>();
// aweXpect: fails, as the delegate threw an AggregateException
await That(callback).Throws<ArgumentException>();
```

Expect the aggregate and its inner exception explicitly:

```csharp
Action callback = // ...

await That(callback).Throws<AggregateException>().WithInner<ArgumentException>();
```

#### Canceled tasks do not complete

FluentAssertions treats a canceled task as completed, so `CompleteWithinAsync(timeout)` passes for it. aweXpect fails `ExecutesIn().AtMost(timeout)` for a cancellation, because it aborts the execution instead of timing it.

```csharp
Func<Task> callback = () => Task.FromCanceled(new CancellationToken(true));

// FluentAssertions: passes
await callback.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
// aweXpect: fails, as the task was canceled
await That(callback).ExecutesIn().AtMost(TimeSpan.FromSeconds(1));
```

### Dates and times

#### DateTime kinds

A `DateTime` with `DateTimeKind.Local` and one with `DateTimeKind.Utc` describe different instants for the same ticks; FluentAssertions ignores the kind. aweXpect does not guess which instant was meant: the ordering expectations (`IsAfter`, `IsBefore` and their variants) fail for such a pair in both polarities; `IsEqualTo` fails and `IsNotEqualTo` passes, with or without `Within(...)`, even for the same ticks; `IsOneOf` ignores a value of the other kind; and `IsInAscendingOrder` fails for a collection that mixes both kinds. `DateTimeKind.Unspecified` is compatible with both.

```csharp
DateTime subject = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
DateTime expected = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Local);

// FluentAssertions: passes, as it compares only 12:00 with 11:00
subject.Should().BeAfter(expected);
// aweXpect: fails
await That(subject).IsAfter(expected);

// FluentAssertions: fails
subject.Should().NotBeAfter(expected);
// aweXpect: fails as well
await That(subject).IsNotAfter(expected);

DateTime local = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);

// FluentAssertions: passes, as it compares only 12:00 with 12:00
subject.Should().Be(local);
// aweXpect: fails
await That(subject).IsEqualTo(local);
```

Convert the values before comparing them:

```csharp
DateTime subject = // ...
DateTime expected = // ...

await That(subject.ToUniversalTime()).IsAfter(expected.ToUniversalTime());
```
