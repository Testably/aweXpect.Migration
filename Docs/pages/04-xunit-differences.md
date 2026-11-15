---
sidebar_label: xUnit differences
description: The cases in which a rewritten xUnit assertion passes or fails for different inputs than the original, and how to restore the original behavior.
---

# Behavioral differences from xUnit

Compared with xUnit 3, a few rewritten assertions pass or fail for different inputs than the original. These follow from the same rules as the [differences from FluentAssertions](./03-fluentassertions-differences.md).

## Nested collections and sets

`Assert.Equal` compares nested collections item by item and a set without regard to order. `IsEqualTo` compares the items of a collection with their `Equals` and in the order in which the collection enumerates them, also for a `HashSet<T>`. Use equivalency for nested collections and `InAnyOrder()` for sets:

```csharp
int[][] actual = // ...
int[][] expected = // ...
HashSet<int> actualSet = // ...
HashSet<int> expectedSet = // ...

await That(actual).IsEquivalentTo(expected);
await That(actualSet).IsEqualTo(expectedSet).InAnyOrder();
```

## Numbers of different types

`Assert.Equal((object)1, (object)1L)` fails, while `IsEqualTo` on an `object` compares numbers by value and passes.

## DateTime kinds

`Assert.Equal` ignores the kind of a `DateTime`, with or without a precision. aweXpect does not compare a `DateTimeKind.Local` value with a `DateTimeKind.Utc` value, because the same ticks describe different instants, so the rewrite fails for such a pair. Convert the values with `ToUniversalTime()` first.

## Empty substrings throw

`Assert.Contains("", actual)`, `Assert.StartsWith("", actual)` and `Assert.EndsWith("", actual)` pass for every string. The rewrites throw an `ArgumentException`, because an expectation that cannot fail checks nothing.

## Negated inspections fail for null

`Assert.DoesNotContain("Yesterday", actual)` passes for a `null` string, and `Assert.IsNotType<T>(null)` and `Assert.IsNotAssignableFrom<T>(null)` pass as well. The rewrites fail, because `null` has no content or type to inspect. Allow `null` explicitly where it is acceptable:

```csharp
string? actual = // ...

await That(actual).IsNull().Or.DoesNotContain("Yesterday");
```
