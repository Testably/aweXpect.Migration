---
description: The xUnit assertions that aweXpect.Migration rewrites to aweXpect, and the cases in which the rewrite behaves differently.
---

import ApplyFix from '@site/src/components/ApplyFix';

# xUnit

The fixer rewrites each `Xunit.Assert.*` call into the corresponding
`Expect.That(actual).<Assertion>(expected)` form. The argument order in xUnit
(`Assert.Equal(expected, actual)`) is swapped to match the "actual first" convention of aweXpect.

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
string title = // ...
string[] tracks = // ...
Dictionary<string, int> playCounts = // ...
double duration = // ...
object album = // ...
Action play = // ...

// highlight-start
Assert.NotNull(title);
Assert.StartsWith("Abbey", title);
Assert.Equal(17, tracks.Length);
Assert.Contains("Come Together", tracks);
Assert.DoesNotContain("Yesterday", tracks);
Assert.Contains("Something", playCounts);
Assert.True(duration > 0, "the album has a duration");
Assert.Equal(47.1, duration, 1);
Assert.IsType<string>(album);
Assert.Throws<InvalidOperationException>(play);
// highlight-end
```

```csharp title="After (aweXpect)"
string title = // ...
string[] tracks = // ...
Dictionary<string, int> playCounts = // ...
double duration = // ...
object album = // ...
Action play = // ...

// highlight-start
Expect.That(title).IsNotNull();
Expect.That(title).StartsWith("Abbey");
Expect.That(tracks.Length).IsEqualTo(17);
Expect.That(tracks).Contains("Come Together");
Expect.That(tracks).DoesNotContain("Yesterday");
Expect.That(playCounts).ContainsKey("Something");
Expect.That(duration > 0).IsTrue().Because("the album has a duration");
Expect.That(Math.Round(duration, 1)).IsEqualTo(Math.Round(47.1, 1));
Expect.That(album).IsExactly<string>();
Expect.That(play).ThrowsExactly<InvalidOperationException>();
// highlight-end
```

</ApplyFix>

The examples show the code as the fix writes it; the `await` is added in a [second step](./00-index.md#how-it-works).

A note in the last column marks a rewrite that behaves differently in some cases; it links to the matching entry under [Behavioral differences](#behavioral-differences).

## Basic

| xUnit construct                    | Rewritten to                                |
|------------------------------------|---------------------------------------------|
| `Assert.Fail("msg")`               | `Fail.Test("msg")`                          |
| `Assert.Skip("msg")`               | `Skip.Test("msg")`                          |
| `Assert.Null(subject)`             | `Expect.That(subject).IsNull()`             |
| `Assert.NotNull(subject)`          | `Expect.That(subject).IsNotNull()`          |
| `Assert.Same(expected, actual)`    | `Expect.That(actual).IsSameAs(expected)`    |
| `Assert.NotSame(expected, actual)` | `Expect.That(actual).IsNotSameAs(expected)` |

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
// highlight-next-line
Assert.Fail("The album has no tracks");
```

```csharp title="After (aweXpect)"
// highlight-next-line
Fail.Test("The album has no tracks");
```

</ApplyFix>

## Booleans

| xUnit construct                | Rewritten to                                    |
|--------------------------------|-------------------------------------------------|
| `Assert.True(subject)`         | `Expect.That(subject).IsTrue()`                 |
| `Assert.False(subject)`        | `Expect.That(subject).IsFalse()`                |
| `Assert.True(subject, "msg")`  | `Expect.That(subject).IsTrue().Because("msg")`  |
| `Assert.False(subject, "msg")` | `Expect.That(subject).IsFalse().Because("msg")` |

## Equality

| xUnit construct                                                          | Rewritten to                                                                          | Note                                                                                  |
|--------------------------------------------------------------------------|---------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------|
| `Assert.Equal(expected, actual)`                                         | `Expect.That(actual).IsEqualTo(expected)`                                             | [collections](#nested-collections-and-sets), [numbers](#numbers-of-different-types), [kind](#datetime-kinds) |
| `Assert.NotEqual(expected, actual)`                                      | `Expect.That(actual).IsNotEqualTo(expected)`                                          |                                                                                       |
| `Assert.Equal(expected, actual, tolerance)` (double / float / TimeSpan)  | `Expect.That(actual).IsEqualTo(expected).Within(tolerance)`                           | [kind](#datetime-kinds)                                                               |
| `Assert.NotEqual(expected, actual, tolerance)` (double / float)          | `Expect.That(actual).IsNotEqualTo(expected).Within(tolerance)`                        |                                                                                       |
| `Assert.Equal(expected, actual, precision)` (double / float / decimal)   | `Expect.That(Math.Round(actual, precision)).IsEqualTo(Math.Round(expected, precision))`   |                                                                                   |
| `Assert.NotEqual(expected, actual, precision)` (double / float / decimal) | `Expect.That(Math.Round(actual, precision)).IsNotEqualTo(Math.Round(expected, precision))` |                                                                                 |

The precision of xUnit rounds both values to the given number of decimal places, which no tolerance can
express, so the rewrite rounds them the same way. A `MidpointRounding` argument is passed on to
`Math.Round`, and a using for `System` is added when necessary. Any other additional argument, such as a
comparer, `ignoreCase: true` or a `StringComparison` on `Assert.Contains`, leaves the assertion for manual
migration.

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
double duration = // ...

// highlight-next-line
Assert.Equal(47.1, duration, 1);
```

```csharp title="After (aweXpect)"
double duration = // ...

// highlight-next-line
Expect.That(Math.Round(duration, 1)).IsEqualTo(Math.Round(47.1, 1));
```

</ApplyFix>

## Strings

| xUnit construct                             | Rewritten to                                     | Note                                         |
|---------------------------------------------|--------------------------------------------------|----------------------------------------------|
| `Assert.Contains(expected, actual)`         | `Expect.That(actual).Contains(expected)`         | [empty](#empty-substrings-throw)             |
| `Assert.DoesNotContain(unexpected, actual)` | `Expect.That(actual).DoesNotContain(unexpected)` | [null](#negated-inspections-fail-for-null)   |
| `Assert.StartsWith(expected, actual)`       | `Expect.That(actual).StartsWith(expected)`       | [empty](#empty-substrings-throw)             |
| `Assert.EndsWith(expected, actual)`         | `Expect.That(actual).EndsWith(expected)`         | [empty](#empty-substrings-throw)             |
| `Assert.Empty(actual)`                      | `Expect.That(actual).IsEmpty()`                  |                                              |
| `Assert.NotEmpty(actual)`                   | `Expect.That(actual).IsNotEmpty()`               |                                              |

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
string title = // ...

// highlight-start
Assert.Contains("Road", title);
Assert.EndsWith("Road", title);
// highlight-end
```

```csharp title="After (aweXpect)"
string title = // ...

// highlight-start
Expect.That(title).Contains("Road");
Expect.That(title).EndsWith("Road");
// highlight-end
```

</ApplyFix>

## Collections

| xUnit construct                                 | Rewritten to                                         |
|-------------------------------------------------|------------------------------------------------------|
| `Assert.Distinct(actual)`                       | `Expect.That(actual).All().AreUnique()`              |
| `Assert.Contains(expected, collection)`         | `Expect.That(collection).Contains(expected)`         |
| `Assert.Contains(collection, predicate)`        | `Expect.That(collection).Contains(predicate)`        |
| `Assert.DoesNotContain(unexpected, collection)` | `Expect.That(collection).DoesNotContain(unexpected)` |
| `Assert.Contains(key, dictionary)`              | `Expect.That(dictionary).ContainsKey(key)`           |
| `Assert.DoesNotContain(key, dictionary)`        | `Expect.That(dictionary).DoesNotContainKey(key)`     |
| `Assert.Empty(collection)`                      | `Expect.That(collection).IsEmpty()`                  |
| `Assert.NotEmpty(collection)`                   | `Expect.That(collection).IsNotEmpty()`               |

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
Dictionary<string, int> playCounts = // ...

// highlight-start
Assert.Contains("Something", playCounts);
Assert.DoesNotContain("Yesterday", playCounts);
// highlight-end
```

```csharp title="After (aweXpect)"
Dictionary<string, int> playCounts = // ...

// highlight-start
Expect.That(playCounts).ContainsKey("Something");
Expect.That(playCounts).DoesNotContainKey("Yesterday");
// highlight-end
```

</ApplyFix>

## Exceptions

| xUnit construct                                                   | Rewritten to                                                  |
|-------------------------------------------------------------------|---------------------------------------------------------------|
| `Assert.Throws<T>(callback)` / `ThrowsAsync<T>(callback)`         | `Expect.That(callback).ThrowsExactly<T>()`                    |
| `Assert.Throws<T>(paramName, callback)` / `ThrowsAsync<T>(...)`   | `Expect.That(callback).ThrowsExactly<T>().WithParamName(paramName)` |
| `Assert.Throws(typeof(T), callback)`                              | `Expect.That(callback).ThrowsExactly(typeof(T))`              |
| `Assert.ThrowsAny<T>(callback)` / `ThrowsAnyAsync<T>(callback)`   | `Expect.That(callback).Throws<T>()`                           |

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
Action play = // ...

// highlight-start
Assert.Throws<InvalidOperationException>(play);
Assert.ThrowsAny<InvalidOperationException>(play);
// highlight-end
```

```csharp title="After (aweXpect)"
Action play = // ...

// highlight-start
Expect.That(play).ThrowsExactly<InvalidOperationException>();
Expect.That(play).Throws<InvalidOperationException>();
// highlight-end
```

</ApplyFix>

## Types

| xUnit construct                                      | Rewritten to                            | Note                                        |
|------------------------------------------------------|-----------------------------------------|---------------------------------------------|
| `Assert.IsAssignableFrom<T>(actual)`                 | `Expect.That(actual).Is<T>()`           |                                             |
| `Assert.IsNotAssignableFrom<T>(actual)`              | `Expect.That(actual).IsNot<T>()`        | [null](#negated-inspections-fail-for-null)  |
| `Assert.IsType<T>(actual)`                           | `Expect.That(actual).IsExactly<T>()`    |                                             |
| `Assert.IsType<T>(actual, exactMatch: false)`        | `Expect.That(actual).Is<T>()`           |                                             |
| `Assert.IsNotType<T>(actual)`                        | `Expect.That(actual).IsNotExactly<T>()` | [null](#negated-inspections-fail-for-null)  |
| `Assert.IsNotType<T>(actual, exactMatch: false)`     | `Expect.That(actual).IsNot<T>()`        | [null](#negated-inspections-fail-for-null)  |

The non-generic overloads (`Assert.IsType(typeof(T), actual)` etc.) are migrated to the matching
non-generic aweXpect form. An `exactMatch` argument that is not a `true` or `false` literal leaves the
assertion for manual migration.

<ApplyFix id="aweXpectM003" migrateTo="aweXpect">

```csharp title="Before (xUnit)"
Exception error = // ...

// highlight-start
Assert.IsType<ArgumentException>(error);
Assert.IsType<ArgumentException>(error, exactMatch: false);
// highlight-end
```

```csharp title="After (aweXpect)"
Exception error = // ...

// highlight-start
Expect.That(error).IsExactly<ArgumentException>();
Expect.That(error).Is<ArgumentException>();
// highlight-end
```

</ApplyFix>

## Behavioral differences

Compared with xUnit 3, a few rewritten assertions pass or fail for different inputs than the original. These follow from the rules listed under [Behavioral differences](./01-fluentassertions.md#behavioral-differences) for FluentAssertions.

### Nested collections and sets

`Assert.Equal` compares nested collections item by item and a set without regard to order.
`IsEqualTo` compares the items of a collection with their `Equals` and in the order in which the
collection enumerates them, also for a `HashSet<T>`. Use equivalency for nested collections and
`InAnyOrder()` for sets:

```csharp
int[][] actual = // ...
int[][] expected = // ...
HashSet<int> actualSet = // ...
HashSet<int> expectedSet = // ...

await That(actual).IsEquivalentTo(expected);
await That(actualSet).IsEqualTo(expectedSet).InAnyOrder();
```

### Numbers of different types

`Assert.Equal((object)1, (object)1L)` fails, while `IsEqualTo` on an `object` compares numbers by value
and passes.

### DateTime kinds

`Assert.Equal` ignores the kind of a `DateTime`, with or without a precision. aweXpect does not compare a
`DateTimeKind.Local` value with a `DateTimeKind.Utc` value, because the same ticks describe different
instants, so the rewrite fails for such a pair. Convert the values with `ToUniversalTime()` first.

### Empty substrings throw

`Assert.Contains("", actual)`, `Assert.StartsWith("", actual)` and `Assert.EndsWith("", actual)` pass for
every string. The rewrites throw an `ArgumentException`, because an expectation that cannot fail checks
nothing.

### Negated inspections fail for null

`Assert.DoesNotContain("x", actual)` passes for a `null` string, and `Assert.IsNotType<T>(null)` and
`Assert.IsNotAssignableFrom<T>(null)` pass as well. The rewrites fail, because `null` has no content or
type to inspect. Allow `null` explicitly where it is acceptable:

```csharp
string? actual = // ...

await That(actual).IsNull().Or.DoesNotContain("x");
```
