# xUnit

The fixer rewrites each `Xunit.Assert.*` call into the corresponding
`Expect.That(actual).<Assertion>(expected)` form. The argument order in xUnit
(`Assert.Equal(expected, actual)`) is swapped to match the "actual first" convention of aweXpect.

```csharp
// Before (xUnit)
Assert.Equal(expected, actual);
Assert.Throws<ArgumentException>(callback);

// After (aweXpect)
await Expect.That(actual).IsEqualTo(expected);
await Expect.That(callback).ThrowsExactly<ArgumentException>();
```

A note in the last column marks a rewrite that behaves differently in some cases; it links to the
matching entry under [Behavioural differences](#behavioural-differences).

## Basic

| xUnit construct                    | Rewritten to                                |
|------------------------------------|---------------------------------------------|
| `Assert.Fail("msg")`               | `Fail.Test("msg")`                          |
| `Assert.Skip("msg")`               | `Skip.Test("msg")`                          |
| `Assert.Null(subject)`             | `Expect.That(subject).IsNull()`             |
| `Assert.NotNull(subject)`          | `Expect.That(subject).IsNotNull()`          |
| `Assert.Same(expected, actual)`    | `Expect.That(actual).IsSameAs(expected)`    |
| `Assert.NotSame(expected, actual)` | `Expect.That(actual).IsNotSameAs(expected)` |

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

## Strings

| xUnit construct                             | Rewritten to                                     | Note                                         |
|---------------------------------------------|--------------------------------------------------|----------------------------------------------|
| `Assert.Contains(expected, actual)`         | `Expect.That(actual).Contains(expected)`         | [empty](#empty-substrings-throw)             |
| `Assert.DoesNotContain(unexpected, actual)` | `Expect.That(actual).DoesNotContain(unexpected)` | [null](#negated-inspections-fail-for-null)   |
| `Assert.StartsWith(expected, actual)`       | `Expect.That(actual).StartsWith(expected)`       | [empty](#empty-substrings-throw)             |
| `Assert.EndsWith(expected, actual)`         | `Expect.That(actual).EndsWith(expected)`         | [empty](#empty-substrings-throw)             |
| `Assert.Empty(actual)`                      | `Expect.That(actual).IsEmpty()`                  |                                              |
| `Assert.NotEmpty(actual)`                   | `Expect.That(actual).IsNotEmpty()`               |                                              |

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

## Exceptions

| xUnit construct                                                   | Rewritten to                                                  |
|-------------------------------------------------------------------|---------------------------------------------------------------|
| `Assert.Throws<T>(callback)` / `ThrowsAsync<T>(callback)`         | `Expect.That(callback).ThrowsExactly<T>()`                    |
| `Assert.Throws<T>(paramName, callback)` / `ThrowsAsync<T>(...)`   | `Expect.That(callback).ThrowsExactly<T>().WithParamName(paramName)` |
| `Assert.Throws(typeof(T), callback)`                              | `Expect.That(callback).ThrowsExactly(typeof(T))`              |
| `Assert.ThrowsAny<T>(callback)` / `ThrowsAnyAsync<T>(callback)`   | `Expect.That(callback).Throws<T>()`                           |

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

## Behavioural differences

Compared with xUnit 3, a few rewritten assertions pass or fail for different inputs than the original.
These follow from the rules listed under
[Behavioural differences](01-fluentassertions.md#behavioural-differences) for FluentAssertions.

### Nested collections and sets

`Assert.Equal` compares nested collections item by item and a set without regard to order.
`IsEqualTo` compares the items of a collection with their `Equals` and in the order in which the
collection enumerates them, also for a `HashSet<T>`. Use equivalency for nested collections and
`InAnyOrder()` for sets:

```csharp
await Expect.That(actual).IsEquivalentTo(expected);
await Expect.That(actualSet).IsEqualTo(expectedSet).InAnyOrder();
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
await Expect.That(actual).IsNull().Or.DoesNotContain("x");
```
