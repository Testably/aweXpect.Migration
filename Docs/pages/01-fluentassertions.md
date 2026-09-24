# FluentAssertions

The fixer rewrites the entire `.Should()` chain in one step: the `Should()` call is dropped,
`Expect.That(subject)` becomes the new root, the assertion methods are renamed, and any trailing
`because` argument is moved into a `.Because(...)` suffix. `.And` chains are preserved.

```csharp
// Before (FluentAssertions)
subject.Should().Contain(1).And.Contain(2, "because foo");

// After (aweXpect)
await Expect.That(subject).Contains(1).And.Contains(2).Because("because foo");
```

The tables below are grouped by **target concept** (equality, containment and so on), not by the
FluentAssertions documentation pages. Most of the basic assertions (`Be`, `NotBe`, `BeEmpty`, `Contain`,
`StartWith` and others) are not specific to one subject type; they apply to any compatible subject.

A note in the last column marks a rewrite that behaves differently in some cases; it links to the
matching entry under [Behavioural differences](#behavioural-differences).

## Equality

| FluentAssertions construct                                  | Rewritten to                                                    | Note                                          |
|-------------------------------------------------------------|-----------------------------------------------------------------|-----------------------------------------------|
| `subject.Should().Be(expected)`                             | `Expect.That(subject).IsEqualTo(expected)`                      |                                               |
| `subject.Should().NotBe(unexpected)`                        | `Expect.That(subject).IsNotEqualTo(unexpected)`                 |                                               |
| `subject.Should().BeApproximately(expected, tolerance)`     | `Expect.That(subject).IsEqualTo(expected).Within(tolerance)`    | [NaN](#nan-is-an-ordinary-value)              |
| `subject.Should().NotBeApproximately(expected, tolerance)`  | `Expect.That(subject).IsNotEqualTo(expected).Within(tolerance)` | [NaN](#nan-is-an-ordinary-value)              |
| `subject.Should().BeCloseTo(expected, delta)`               | `Expect.That(subject).IsEqualTo(expected).Within(delta)`        | [delta](#integral-tolerances)                 |
| `subject.Should().NotBeCloseTo(expected, delta)`            | `Expect.That(subject).IsNotEqualTo(expected).Within(delta)`     | [delta](#integral-tolerances)                 |
| `subject.Should().BeOneOf(a, b, c)`                         | `Expect.That(subject).IsOneOf(a, b, c)`                         | [numbers](#numbers-of-different-types)        |
| `subject.Should().BeOneOf(collection)`                      | `Expect.That(subject).IsOneOf(collection)`                      | [numbers](#numbers-of-different-types)        |
| `subject.Should().BeOneOf(value, collection)`               | `Expect.That(subject).IsOneOf(value)`                           |                                               |
| `subject.Should().BeSameAs(expected)`                       | `Expect.That(subject).IsSameAs(expected)`                       |                                               |
| `subject.Should().NotBeSameAs(unexpected)`                  | `Expect.That(subject).IsNotSameAs(unexpected)`                  |                                               |

For an integral subject, FluentAssertions takes the delta of `BeCloseTo` as the unsigned counterpart of the
subject type (`uint` for `int`). When the delta does not convert implicitly, the fixer casts it to the
subject type, e.g. `Within((int)delta)`.

### Equivalency

`Should().BeEquivalentTo(...)` and `Should().NotBeEquivalentTo(...)` dispatch on the called overload and
the type of the expected items:

| Subject                                                          | `BeEquivalentTo(expected)` rewrites to                      |
|------------------------------------------------------------------|-------------------------------------------------------------|
| `string`                                                         | `IsEqualTo(expected).IgnoringCase()`                        |
| collection of items that are compared by value (see below)       | `IsEqualTo(expected).InAnyOrder()`                          |
| any other collection, and any other object                       | `IsEquivalentTo(expected, o => o.IgnoringCollectionOrder())` |

Items are compared by value when FluentAssertions compares them with `Equals` as well: primitives,
enums, strings, structs and classes that override `Equals`, but not records and anonymous types. All
other items are compared structurally, like FluentAssertions does. FluentAssertions also ignores the
order of nested collections, which `IgnoringCollectionOrder()` restores; the fixer adds
`using aweXpect.Equivalency;` for it when necessary. With `o => o.WithStrictOrdering()` the rewrite
omits `InAnyOrder()` and `IgnoringCollectionOrder()`.

For string subjects the `o => o...` options are translated as well. As in FluentAssertions, a string
compared with options ignores case only when the options contain `IgnoringCase()`:

| FluentAssertions option               | Appended to the rewrite         |
|---------------------------------------|---------------------------------|
| `o => o.IgnoringCase()`               | `.IgnoringCase()`               |
| `o => o.IgnoringLeadingWhitespace()`  | `.IgnoringLeadingWhiteSpace()`  |
| `o => o.IgnoringTrailingWhitespace()` | `.IgnoringTrailingWhiteSpace()` |
| `o => o.IgnoringNewlineStyle()`       | `.IgnoringNewlineStyle()`       |

For structural comparisons, options other than `WithStrictOrdering()` and `WithoutStrictOrdering()` have
no automatic translation, so no code fix is offered. `NotBeEquivalentTo` is translated symmetrically
(`IsNotEqualTo` / `IsNotEquivalentTo`). See also [Structural equivalency](#structural-equivalency) under the
behavioural differences.

## Null and emptiness

| FluentAssertions construct                              | Rewritten to                                     | Note                                        |
|---------------------------------------------------------|--------------------------------------------------|---------------------------------------------|
| `subject.Should().BeNull()`                             | `Expect.That(subject).IsNull()`                  |                                             |
| `subject.Should().NotBeNull()`                          | `Expect.That(subject).IsNotNull()`               |                                             |
| `subject.Should().BeEmpty()`                            | `Expect.That(subject).IsEmpty()`                 |                                             |
| `subject.Should().NotBeEmpty()`                         | `Expect.That(subject).IsNotEmpty()`              | [null](#negated-inspections-fail-for-null)  |
| `subject.Should().BeNullOrEmpty()` *(string)*           | `Expect.That(subject).IsNullOrEmpty()`           |                                             |
| `subject.Should().BeNullOrEmpty()` *(collection)*       | `Expect.That(subject).IsNull().Or.IsEmpty()`     |                                             |
| `subject.Should().NotBeNullOrEmpty()` *(string)*        | `Expect.That(subject).IsNotNullOrEmpty()`        |                                             |
| `subject.Should().NotBeNullOrEmpty()` *(collection)*    | `Expect.That(subject).IsNotEmpty()`              |                                             |
| `subject.Should().BeNullOrWhiteSpace()`                 | `Expect.That(subject).IsNullOrWhiteSpace()`      |                                             |
| `subject.Should().NotBeNullOrWhiteSpace()`              | `Expect.That(subject).IsNotNullOrWhiteSpace()`   |                                             |

## Booleans

| FluentAssertions construct      | Rewritten to                          |
|---------------------------------|---------------------------------------|
| `subject.Should().BeTrue()`     | `Expect.That(subject).IsTrue()`       |
| `subject.Should().BeFalse()`    | `Expect.That(subject).IsFalse()`      |
| `subject.Should().NotBeTrue()`  | `Expect.That(subject).IsNotTrue()`    |
| `subject.Should().NotBeFalse()` | `Expect.That(subject).IsNotFalse()`   |
| `subject.Should().Imply(other)` | `Expect.That(subject).Implies(other)` |

## Type checks

The generic forms (`<T>`) and the non-generic `(typeof(T))` overloads are both supported; the fixer keeps
whichever form was used.

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

On a `System.Type` subject, `BeAssignableTo` and `NotBeAssignableTo` are not rewritten; see
[Assertions on a Type](#assertions-on-a-type).

## Numbers

| FluentAssertions construct                                      | Rewritten to                                            | Note                                   |
|-----------------------------------------------------------------|---------------------------------------------------------|----------------------------------------|
| `subject.Should().BePositive()`                                 | `Expect.That(subject).IsPositive()`                     |                                        |
| `subject.Should().BeNegative()`                                 | `Expect.That(subject).IsNegative()`                     |                                        |
| `subject.Should().BeGreaterThan(expected)`                      | `Expect.That(subject).IsGreaterThan(expected)`          |                                        |
| `subject.Should().BeGreaterThanOrEqualTo(expected)`             | `Expect.That(subject).IsGreaterThanOrEqualTo(expected)` |                                        |
| `subject.Should().BeGreaterOrEqualTo(expected)` *(legacy FA 7)* | `Expect.That(subject).IsGreaterThanOrEqualTo(expected)` |                                        |
| `subject.Should().BeLessThan(expected)`                         | `Expect.That(subject).IsLessThan(expected)`             |                                        |
| `subject.Should().BeLessThanOrEqualTo(expected)`                | `Expect.That(subject).IsLessThanOrEqualTo(expected)`    |                                        |
| `subject.Should().BeLessOrEqualTo(expected)` *(legacy FA 7)*    | `Expect.That(subject).IsLessThanOrEqualTo(expected)`    |                                        |
| `subject.Should().BeInRange(low, high)`                         | `Expect.That(subject).IsBetween(low).And(high)`         | [range](#reversed-ranges-throw)        |
| `subject.Should().NotBeInRange(low, high)`                      | `Expect.That(subject).IsNotBetween(low).And(high)`      | [range](#reversed-ranges-throw)        |

## Containment, ordering and predicates

These cover both string and collection subjects; the fixer dispatches on the called overload where it
matters, so an item that is itself a collection is still treated as a single item.

| FluentAssertions construct                                              | Rewritten to                                                                                              | Note                                        |
|-------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|---------------------------------------------|
| `subject.Should().Contain(expected)`                                    | `Expect.That(subject).Contains(expected)`                                                                 |                                             |
| `subject.Should().Contain(item).And.Contain(other)`                     | `Expect.That(subject).Contains(item).And.Contains(other)`                                                 |                                             |
| `subject.Should().Contain(collection)`                                  | `Expect.That(subject).Contains(collection).InAnyOrder().IgnoringInterspersedItems().IgnoringDuplicates()` |                                             |
| `subject.Should().NotContain(unexpected)`                               | `Expect.That(subject).DoesNotContain(unexpected)`                                                         | [null](#negated-inspections-fail-for-null)  |
| `subject.Should().NotContain(collection)`                               | `Expect.That(subject).None().ComplyWith(x => x.IsOneOf(collection))`                                      | [numbers](#numbers-of-different-types)      |
| `subject.Should().StartWith(expected)`                                  | `Expect.That(subject).StartsWith(expected)`                                                               | [empty](#empty-expectations-throw)          |
| `subject.Should().NotStartWith(unexpected)`                             | `Expect.That(subject).DoesNotStartWith(unexpected)`                                                       |                                             |
| `subject.Should().EndWith(expected)`                                    | `Expect.That(subject).EndsWith(expected)`                                                                 | [empty](#empty-expectations-throw)          |
| `subject.Should().NotEndWith(unexpected)`                               | `Expect.That(subject).DoesNotEndWith(unexpected)`                                                         |                                             |
| `subject.Should().ContainInOrder(collection)`                           | `Expect.That(subject).Contains(collection).IgnoringInterspersedItems()`                                   | [empty](#empty-expectations-throw)          |
| `subject.Should().ContainInOrder(a, b, c)`                              | `Expect.That(subject).Contains([a, b, c]).IgnoringInterspersedItems()`                                    |                                             |
| `subject.Should().ContainInConsecutiveOrder(collection)`                | `Expect.That(subject).Contains(collection)`                                                               | [empty](#empty-expectations-throw)          |
| `subject.Should().ContainInConsecutiveOrder(a, b, c)`                   | `Expect.That(subject).Contains([a, b, c])`                                                                |                                             |
| `subject.Should().ContainEquivalentOf(expected)`                        | `Expect.That(subject).Contains(expected).Equivalent(o => o.IgnoringCollectionOrder())`                    | [equivalency](#structural-equivalency)        |
| `subject.Should().NotContainEquivalentOf(unexpected)`                   | `Expect.That(subject).DoesNotContain(unexpected).Equivalent(o => o.IgnoringCollectionOrder())`            | [equivalency](#structural-equivalency)        |
| `subject.Should().BeSubsetOf(expected)`                                 | `Expect.That(subject).IsContainedIn(expected).InAnyOrder().IgnoringDuplicates()`                          |                                             |
| `subject.Should().NotBeSubsetOf(expected)`                              | `Expect.That(subject).IsNotContainedIn(expected).InAnyOrder()`                                            |                                             |
| `subject.Should().HaveCount(n)`                                         | `Expect.That(subject).HasCount(n)`                                                                        |                                             |
| `subject.Should().ContainSingle()`                                      | `Expect.That(subject).HasSingle()`                                                                        |                                             |
| `subject.Should().ContainSingle(predicate)`                             | `Expect.That(subject).HasSingle().Matching(predicate)`                                                    |                                             |
| `subject.Should().OnlyContain(predicate)`                               | `Expect.That(subject).All().Satisfy(predicate)`                                                           |                                             |
| `subject.Should().OnlyHaveUniqueItems()`                                | `Expect.That(subject).All().AreUnique()`                                                                  |                                             |
| `subject.Should().BeInAscendingOrder()`                                 | `Expect.That(subject).IsInAscendingOrder()`                                                               |                                             |
| `subject.Should().BeInAscendingOrder(comparer)`                         | `Expect.That(subject).IsInAscendingOrder().Using(comparer)`                                               |                                             |
| `subject.Should().BeInAscendingOrder(x => x.Key)`                       | `Expect.That(subject).IsInAscendingOrder(x => x.Key)`                                                     |                                             |
| `subject.Should().BeInDescendingOrder()`                                | `Expect.That(subject).IsInDescendingOrder()`                                                              |                                             |
| `subject.Should().NotBeInAscendingOrder()` / `NotBeInDescendingOrder()` | `Expect.That(subject).IsNotInAscendingOrder()` / `IsNotInDescendingOrder()`                               |                                             |
| `subject.Should().Match(predicate)` *(non-string subject)*              | `Expect.That(subject).Satisfies(predicate)`                                                               |                                             |
| `subject.Should().Match(pattern)` *(string subject)*                    | `Expect.That(subject).IsEqualTo(pattern).AsWildcard()`                                                    |                                             |

`ContainEquivalentOf` and `NotContainEquivalentOf` use `Equivalent()` without options for items that are
compared by value (see [Equivalency](#equivalency)) and with `o => o.WithStrictOrdering()`.

### Occurrence constraints on `Contain` (string subjects)

`Should().Contain(expected, occurrence)` translates the full FluentAssertions occurrence family. The
`Thrice` and `Times(n)` cases are emitted as `3.Times()` or numeric arguments, not as a chained
`.Thrice()`. Occurrences are [counted without overlap](#occurrences-are-counted-without-overlap).

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

## Collections

In addition to the containment and ordering assertions above, these collection-specific forms are
recognised:

| FluentAssertions construct                                               | Rewritten to                                                                    | Note                                     |
|--------------------------------------------------------------------------|---------------------------------------------------------------------------------|------------------------------------------|
| `subject.Should().AllBeAssignableTo<T>()` / `(typeof(T))`                | `Expect.That(subject).All().Are<T>()` / `All().Are(typeof(T))`                  | [Type items](#type-instances-as-items)   |
| `subject.Should().AllBeOfType<T>()` / `(typeof(T))`                      | `Expect.That(subject).All().AreExactly<T>()` / `All().AreExactly(typeof(T))`    | [Type items](#type-instances-as-items)   |
| `subject.Should().AllBeEquivalentTo(expected)` *(non-string elements)*   | `Expect.That(subject).All().AreEquivalentTo(expected, o => o.IgnoringCollectionOrder())` | [equivalency](#structural-equivalency) |
| `subject.Should().AllBeEquivalentTo(expected)` *(`IEnumerable<string>`)* | `Expect.That(subject).All().AreEqualTo(expected)`                               |                                          |

For elements that are compared by value (see [Equivalency](#equivalency)) or with
`o => o.WithStrictOrdering()`, `AllBeEquivalentTo` is rewritten without the options lambda. For
`IEnumerable<string>` collections, the `o => o...` options are translated as well:

| FluentAssertions option               | Appended to the rewrite         |
|---------------------------------------|---------------------------------|
| `o => o.IgnoringCase()`               | `.IgnoringCase()`               |
| `o => o.IgnoringLeadingWhitespace()`  | `.IgnoringLeadingWhiteSpace()`  |
| `o => o.IgnoringTrailingWhitespace()` | `.IgnoringTrailingWhiteSpace()` |
| `o => o.IgnoringNewlineStyle()`       | `.IgnoringNewlineStyle()`       |
| `o => o.WithoutStrictOrdering()`      | `.InAnyOrder()`                 |

## Object equivalency

| FluentAssertions construct                                            | Rewritten to                                                                      | Note                                  |
|-----------------------------------------------------------------------|-----------------------------------------------------------------------------------|---------------------------------------|
| `subject.Should().BeEquivalentTo(expected)` *(arbitrary object)*      | `Expect.That(subject).IsEquivalentTo(expected, o => o.IgnoringCollectionOrder())`      | [equivalency](#structural-equivalency) |
| `subject.Should().NotBeEquivalentTo(unexpected)` *(arbitrary object)* | `Expect.That(subject).IsNotEquivalentTo(unexpected, o => o.IgnoringCollectionOrder())` | [equivalency](#structural-equivalency) |

## Exceptions

| FluentAssertions construct                                       | Rewritten to                                                 | Note                                              |
|------------------------------------------------------------------|--------------------------------------------------------------|---------------------------------------------------|
| `callback.Should().NotThrow()` / `NotThrowAsync()`               | `Expect.That(callback).DoesNotThrow()`                       |                                                   |
| `callback.Should().NotThrow<T>()` / `NotThrowAsync<T>()`         | `Expect.That(callback).DoesNotThrow<T>()`                    | [aggregate](#aggregateexception-is-not-unwrapped) |
| `callback.Should().Throw<T>()` / `ThrowAsync<T>()`               | `Expect.That(callback).Throws<T>()`                          | [aggregate](#aggregateexception-is-not-unwrapped) |
| `callback.Should().ThrowExactly<T>()` / `ThrowExactlyAsync<T>()` | `Expect.That(callback).ThrowsExactly<T>()`                   |                                                   |
| `.WithMessage(pattern)`                                          | `.WithMessage(pattern).AsWildcard().IgnoringCase().IgnoringNewlineStyle()` |                                     |
| `.WithInnerException<T>()` / `(typeof(T))`                       | `.WithInner<T>()` / `.WithInner(typeof(T))`                  |                                                   |
| `.WithParameterName(name)`                                       | `.WithParamName(name)`                                       |                                                   |

```csharp
// Before
callback.Should().Throw<ArgumentException>().WithMessage("foo*");

// After
await Expect.That(callback).Throws<ArgumentException>().WithMessage("foo*").AsWildcard().IgnoringCase().IgnoringNewlineStyle();
```

A chained call without an aweXpect counterpart, such as `Where(...)` or `WithInnerExceptionExactly<T>()`,
leaves the assertion for manual migration. So does a call chained after `WithInnerException`, because
FluentAssertions continues on the inner exception there, whereas `WithInner` returns to the outer one.

## Dates and times

| FluentAssertions construct                                                            | Rewritten to                                                            | Note                                 |
|---------------------------------------------------------------------------------------|-------------------------------------------------------------------------|--------------------------------------|
| `subject.Should().BeAfter(expected)`                                                  | `Expect.That(subject).IsAfter(expected)`                                | [kind](#datetime-kinds)              |
| `subject.Should().BeOnOrAfter(expected)`                                              | `Expect.That(subject).IsOnOrAfter(expected)`                            | [kind](#datetime-kinds)              |
| `subject.Should().BeBefore(expected)`                                                 | `Expect.That(subject).IsBefore(expected)`                               | [kind](#datetime-kinds)              |
| `subject.Should().BeOnOrBefore(expected)`                                             | `Expect.That(subject).IsOnOrBefore(expected)`                           | [kind](#datetime-kinds)              |
| `subject.Should().NotBeAfter(unexpected)` / `NotBeOnOrAfter(...)`                     | `Expect.That(subject).IsNotAfter(unexpected)` / `IsNotOnOrAfter(...)`   | [kind](#datetime-kinds)              |
| `subject.Should().NotBeBefore(unexpected)` / `NotBeOnOrBefore(...)`                   | `Expect.That(subject).IsNotBefore(unexpected)` / `IsNotOnOrBefore(...)` | [kind](#datetime-kinds)              |
| `subject.Should().HaveYear(n)` (also `Month`/`Day`/`Hour`/`Minute`/`Second`/`Offset`) | `Expect.That(subject).HasYear().EqualTo(n)` (etc.)                      |                                      |
| `subject.Should().NotHaveYear(n)` (and the other parts)                               | `Expect.That(subject).HasYear().NotEqualTo(n)` (etc.)                   |                                      |

`BeCloseTo`, `NotBeCloseTo` and `BeOneOf` on dates are listed under [Equality](#equality); the
[kind](#datetime-kinds) of a `DateTime` matters there as well.

## Enums and nullable values

| FluentAssertions construct                     | Rewritten to                                     | Note                                        |
|------------------------------------------------|--------------------------------------------------|---------------------------------------------|
| `subject.Should().BeDefined()`                 | `Expect.That(subject).IsDefined()`               |                                             |
| `subject.Should().NotBeDefined()`              | `Expect.That(subject).IsNotDefined()`            |                                             |
| `subject.Should().HaveFlag(flag)`              | `Expect.That(subject).HasFlag(flag)`             |                                             |
| `subject.Should().NotHaveFlag(flag)`           | `Expect.That(subject).DoesNotHaveFlag(flag)`     |                                             |
| `subject.Should().HaveValue()` (`Nullable<T>`) | `Expect.That(subject).IsNotNull()`               |                                             |
| `subject.Should().HaveValue(v)`                | `Expect.That(subject).HasValue(v)`               |                                             |
| `subject.Should().NotHaveValue()`              | `Expect.That(subject).IsNull()`                  |                                             |
| `subject.Should().NotHaveValue(v)`             | `Expect.That(subject).HasValue().NotEqualTo(v)`  | [null](#negated-inspections-fail-for-null)  |

## Lambda assertions

When the entire `.Should()` chain is the body of a lambda (so it is not awaited at the call site and the
surrounding signature is `Action` or `Func<T>`), the fixer wraps the rewritten expression in
`aweXpect.Synchronous.Synchronously.Verify(...)` to keep the lambda synchronous:

```csharp
// Before
Action action = () => true.Should().BeTrue();

// After
Action action = () => aweXpect.Synchronous.Synchronously.Verify(Expect.That(true).IsTrue());
```

## Because messages

A trailing `because` argument on any of the assertions above is preserved as a `.Because(...)` suffix,
including formatted overloads:

```csharp
// Before
subject.Should().Be(expected, "because the value is {0}", value);

// After
await Expect.That(subject).IsEqualTo(expected).Because($"because the value is {value}");
```

## Behavioural differences

Compared with FluentAssertions 8, a few rewritten assertions pass or fail for different inputs than the
original. These are choices aweXpect makes on purpose, and the fixer cannot bridge them. Most follow from
a few rules:

- **Strict by default, loosen explicitly.** The default is the strictest reasonable reading, and each
  relaxation is an opt-in with a name, such as `AsWildcard()`, `IgnoringCase()`, `InAnyOrder()` or
  `IgnoringCollectionOrder()`.
- **A negation is the exact complement.** `DoesNotX` passes exactly when `X` fails.
- **`null` never passes an inspection by accident.** Equality treats `null` as a value; expectations that
  inspect the content fail for `null` in both polarities.
- **Impossible or malformed expectations are programming errors.** They throw an argument exception
  instead of failing or passing.
- **No silent guessing.** A comparison that cannot be answered honestly fails in both polarities.
- **What you pass is what is compared.** The runtime type drives equivalency, `Equals` overrides are not
  trusted, numbers of different types are not converted in equivalency, and an `AggregateException` is
  not unwrapped.

### Equality

#### NaN is an ordinary value

`BeApproximately(double.NaN, tolerance)` throws in FluentAssertions and `NotBeApproximately` fails for a
`NaN` subject. The rewrites pass: aweXpect compares `NaN` like `double.Equals` does, as a value that
equals itself and nothing else. Add `IsNotNaN()` where a `NaN` subject has to fail:

```csharp
await Expect.That(subject).IsNotNaN().And.IsNotEqualTo(expected).Within(tolerance);
```

#### Integral tolerances

The rewrite casts an unsigned `BeCloseTo` delta to the subject type. A delta that does not fit, such as
`uint.MaxValue` for an `int`, becomes negative and throws an `ArgumentOutOfRangeException`, because a
negative tolerance is a malformed expectation.

#### Numbers of different types

`IsOneOf` on an `object` subject compares numbers by value, like `IsEqualTo`, so
`Expect.That((object)1).IsOneOf(1L, 2L)` passes where `BeOneOf` fails. The rewrite of
`NotContain(collection)` uses `IsOneOf` as well and therefore also rejects an item `1` for an unexpected
`1L`. aweXpect applies one rule to all equality expectations on `object`.

### Null and emptiness

#### Negated inspections fail for null

`NotBeEmpty()` and `NotContain("x")` pass in FluentAssertions for a `null` string, and `NotHaveValue(2)`
passes for a `null` enum. Their rewrites fail, because an expectation that inspects the content has
nothing to inspect in `null`. Allow `null` explicitly where it is acceptable:

```csharp
await Expect.That(subject).IsNull().Or.IsNotEmpty();
```

### Type checks

#### Assertions on a Type

FluentAssertions checks the type that a `System.Type` subject describes, so
`typeof(string).Should().BeAssignableTo<IComparable>()` passes. aweXpect has no expectations on a
`Type` subject: `Is<IComparable>()` would check the `Type` object itself and silently invert
`NotBeAssignableTo`. No code fix is offered; migrate such assertions by hand:

```csharp
await Expect.That(typeof(IComparable).IsAssignableFrom(subject)).IsTrue();
```

### Numbers

#### Reversed ranges throw

`NotBeInRange(3, 1)` passes in FluentAssertions for every value, and `BeInRange(3, 1)` fails for every
value. `IsNotBetween(3).And(1)` and `IsBetween(3).And(1)` throw an `ArgumentOutOfRangeException`, because a
range whose maximum is below its minimum is a malformed expectation. Swap the bounds.

### Containment

#### Empty expectations throw

`StartWith("")`, `EndWith("")`, and `StartWith`, `ContainInOrder` or `ContainInConsecutiveOrder` with an
empty collection pass in FluentAssertions for every subject. The rewrites throw an `ArgumentException`,
because an expectation that cannot fail checks nothing. Remove the assertion or expect something
concrete.

#### Occurrences are counted without overlap

`Contains(x)` with an occurrence constraint counts non-overlapping occurrences, the way `Regex.Matches`
and a loop over `IndexOf` do, so `"aaaa"` contains `"aa"` exactly twice. FluentAssertions counts every
position at which the substring starts, which is three times.

### Collections

#### Type instances as items

`AllBeAssignableTo<T>()` and `AllBeOfType<T>()` also accept `Type` instances that describe a matching
type. `All().Are<T>()` and `All().AreExactly<T>()` check the runtime type of each item, and the runtime
type of a `Type` object is `RuntimeType`. Check the described types explicitly:

```csharp
await Expect.That(types).All().Satisfy(type => typeof(Exception).IsAssignableFrom(type));
```

### Structural equivalency

The rewrites add `IgnoringCollectionOrder()` to match the FluentAssertions default for nested collections.
The following differences remain:

- **The runtime type of the expectation drives the comparison.** aweXpect compares all public members of
  the expectation's runtime type; FluentAssertions only those of its declared type. Ignore a member that
  should not count with `o => o.IgnoringMember("Name")`.
- **`Equals` overrides are ignored while comparing by members.** FluentAssertions compares a class that
  overrides `Equals` with `Equals`; aweXpect compares its members, so an `Equals` that ignores a member no
  longer hides a difference in it. Ask for the type's own equality explicitly:
  ```csharp
  await Expect.That(subject).IsEquivalentTo(expected, o => o
      .IgnoringCollectionOrder()
      .For<Money>(x => x with { ComparisonType = EquivalencyComparisonType.ByValue }));
  ```
- **Numbers of different types are not converted.** A member of type `int` is not equivalent to a member of
  type `long` with the same value. Use the same type in the expectation, e.g. `30` instead of `30L`.

### Exceptions

#### AggregateException is not unwrapped

FluentAssertions looks through an `AggregateException`, so `Throw<ArgumentException>()` passes for an
aggregate that wraps an `ArgumentException`, and `NotThrow<ArgumentException>()` fails for it. aweXpect
compares the exception the delegate actually threw, so `Throws<ArgumentException>()` fails and
`DoesNotThrow<ArgumentException>()` passes. Expect the aggregate and its inner exception explicitly:

```csharp
await Expect.That(callback).Throws<AggregateException>().WithInner<ArgumentException>();
```

### Dates and times

#### DateTime kinds

A `DateTime` with `DateTimeKind.Local` and one with `DateTimeKind.Utc` describe different instants for
the same ticks; FluentAssertions ignores the kind. aweXpect does not guess which instant was meant: the
ordering expectations (`IsAfter`, `IsBefore` and their variants) fail for such a pair in both polarities,
`IsEqualTo(...).Within(...)` fails and its negation passes, `IsOneOf` ignores a value of the other kind, and
`IsInAscendingOrder` fails for a collection that mixes both kinds. `DateTimeKind.Unspecified` is compatible
with both. Convert the values before comparing them:

```csharp
await Expect.That(subject.ToUniversalTime()).IsAfter(expected.ToUniversalTime());
```
