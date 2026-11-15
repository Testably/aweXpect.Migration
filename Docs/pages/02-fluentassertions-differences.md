---
sidebar_label: FluentAssertions differences
description: The cases in which a rewritten FluentAssertions assertion passes or fails for different inputs than the original, and how to restore the original behavior.
---

# Behavioral differences from FluentAssertions

Compared with FluentAssertions 8, a few rewritten assertions pass or fail for different inputs than the original. These are choices aweXpect makes on purpose, and the code fix cannot bridge them. Most follow from a few rules:

- **Strict by default, loosen explicitly.** The default is the strictest reasonable reading, and each relaxation is an opt-in with a name, such as `AsWildcard()`, `IgnoringCase()`, `InAnyOrder()` or `IgnoringCollectionOrder()`.
- **A negation is the exact complement.** `DoesNotX` passes exactly when `X` fails.
- **`null` never passes an inspection by accident.** Equality treats `null` as a value; expectations that inspect the content fail for `null` in both polarities.
- **Impossible or malformed expectations are programming errors.** They throw an argument exception instead of failing or passing.
- **No silent guessing.** A comparison that cannot be answered honestly fails in both polarities.
- **What you pass is what is compared.** The runtime type drives equivalency, `Equals` overrides are not trusted, numbers and enums of different types are not converted in equivalency, and an `AggregateException` is not unwrapped.

Each example below runs the original assertion and its rewrite on the same input and shows the outcome of both.

## Equality

### NaN is an ordinary value

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

### Integral tolerances

The rewrite casts an unsigned `BeCloseTo` delta to the subject type. A variable delta that does not fit, such as `uint.MaxValue` for an `int`, becomes negative and throws an `ArgumentOutOfRangeException`, because a negative tolerance is a malformed expectation.

```csharp
int subject = 1;
uint delta = uint.MaxValue;

// FluentAssertions: passes
subject.Should().BeCloseTo(1, delta);
// aweXpect: throws an ArgumentOutOfRangeException, as (int)delta is -1
await That(subject).IsEqualTo(1).Within((int)delta);
```

### Numbers of different types

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

## Null and emptiness

### Negated inspections fail for null

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

## Type checks

### Assertions on a `Type`

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

## Numbers

### Reversed ranges throw

`NotBeInRange(3, 1)` passes in FluentAssertions for every value, and `BeInRange(3, 1)` fails for every value. `IsNotBetween(3).And(1)` and `IsBetween(3).And(1)` throw an `ArgumentOutOfRangeException`, because a range whose maximum is below its minimum is a malformed expectation. Swap the bounds.

```csharp
int subject = 2;

// FluentAssertions: passes
subject.Should().NotBeInRange(3, 1);
// aweXpect: throws an ArgumentOutOfRangeException
await That(subject).IsNotBetween(3).And(1);
```

## Containment

### Empty expectations throw

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

### Empty wildcard patterns

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

### Occurrences are counted without overlap

`Contains(x)` with an occurrence constraint counts non-overlapping occurrences, the way `Regex.Matches` and a loop over `IndexOf` do, so `"nananana"` contains `"nana"` exactly twice. FluentAssertions counts every position at which the substring starts, which is three times.

```csharp
string subject = "nananana";

// FluentAssertions: fails, as it counts "nana" three times
subject.Should().Contain("nana", Exactly.Twice());
// aweXpect: passes
await That(subject).Contains("nana").Twice();
```

## Collections

### Type instances as items

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

## Structural equivalency

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
- **`Equals` overrides of nested objects are ignored.** When the type of the expectation itself overrides `Equals`, the rewrite uses `IsEqualTo` (see [Equivalency](./01-fluentassertions.md#equivalency)). A nested object whose type overrides `Equals` is compared differently: FluentAssertions compares it with `Equals`; aweXpect compares its members, so an `Equals` that ignores a member no longer hides a difference in it.
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

## Exceptions

### AggregateException is not unwrapped

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

### Canceled tasks do not complete

FluentAssertions treats a canceled task as completed, so `CompleteWithinAsync(timeout)` passes for it. aweXpect fails `ExecutesIn().AtMost(timeout)` for a cancellation, because it aborts the execution instead of timing it.

```csharp
Func<Task> callback = () => Task.FromCanceled(new CancellationToken(true));

// FluentAssertions: passes
await callback.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
// aweXpect: fails, as the task was canceled
await That(callback).ExecutesIn().AtMost(TimeSpan.FromSeconds(1));
```

## Dates and times

### DateTime kinds

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
