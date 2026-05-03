namespace aweXpect.Migration.Tests.FluentAssertions;

public static class TestCases
{
	/// <summary>
	///     <see href="https://fluentassertions.com/basicassertions/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Basic()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("object subject = new object();",
			"subject.Should().BeNull()",
			"Expect.That(subject).IsNull()");
		theoryData.AddWithBecause("object subject = new object();",
			"subject.Should().NotBeNull()",
			"Expect.That(subject).IsNotNull()");
		theoryData.AddWithBecause("object subject = new object();object expected = new object();",
			"subject.Should().BeSameAs(expected, {0})",
			"Expect.That(subject).IsSameAs(expected)");
		theoryData.AddWithBecause("object subject = new object();object unexpected = new object();",
			"subject.Should().NotBeSameAs(unexpected, {0})",
			"Expect.That(subject).IsNotSameAs(unexpected)");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().BeAssignableTo<ArgumentException>({0})",
			"Expect.That(subject).Is<ArgumentException>()");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().BeAssignableTo(typeof(ArgumentException), {0})",
			"Expect.That(subject).Is(typeof(ArgumentException))");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().NotBeAssignableTo<ArgumentException>({0})",
			"Expect.That(subject).IsNot<ArgumentException>()");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().NotBeAssignableTo(typeof(ArgumentException), {0})",
			"Expect.That(subject).IsNot(typeof(ArgumentException))");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().BeOfType<ArgumentException>({0})",
			"Expect.That(subject).IsExactly<ArgumentException>()");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().BeOfType(typeof(ArgumentException), {0})",
			"Expect.That(subject).IsExactly(typeof(ArgumentException))");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().NotBeOfType<ArgumentException>({0})",
			"Expect.That(subject).IsNotExactly<ArgumentException>()");
		theoryData.AddWithBecause("object subject = new Exception();",
			"subject.Should().NotBeOfType(typeof(ArgumentException), {0})",
			"Expect.That(subject).IsNotExactly(typeof(ArgumentException))");
		theoryData.AddWithBecause("object subject = new();",
			"subject.Should().Match(o => o.GetHashCode() > 0, {0})",
			"Expect.That(subject).Satisfies(o => o.GetHashCode() > 0)");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/booleans/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Boolean()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("bool subject = false;",
			"subject.Should().BeTrue({0})",
			"Expect.That(subject).IsTrue()");
		theoryData.AddWithBecause("bool subject = false;",
			"subject.Should().BeFalse({0})",
			"Expect.That(subject).IsFalse()");
		theoryData.AddWithBecause("bool subject = false;bool expected = false;",
			"subject.Should().Imply(expected, {0})",
			"Expect.That(subject).Implies(expected)");
		theoryData.AddWithBecause("bool? subject = false;",
			"subject.Should().NotBeTrue({0})",
			"Expect.That(subject).IsNotTrue()");
		theoryData.AddWithBecause("bool? subject = null;",
			"subject.Should().NotBeFalse({0})",
			"Expect.That(subject).IsNotFalse()");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/collections/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Collection()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().HaveCount(1, {0})",
			"Expect.That(subject).HasCount(1)");
		theoryData.AddWithBecause("int[] subject = [1, 2,]; int[] expected = [2, 1,];",
			"subject.Should().BeEquivalentTo(expected, {0})",
			"Expect.That(subject).IsEqualTo(expected).InAnyOrder()");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().OnlyContain(x => x > 0, {0})",
			"Expect.That(subject).All().Satisfy(x => x > 0)");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().ContainSingle(x => x > 0, {0})",
			"Expect.That(subject).HasSingle().Matching(x => x > 0)");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().ContainSingle({0})",
			"Expect.That(subject).HasSingle()");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().Contain(1).And.Contain(2, {0})",
			"Expect.That(subject).Contains(1).And.Contains(2)");
		theoryData.AddWithBecause("IEnumerable<int> subject = Enumerable.Range(1, 3);",
			"subject.Should().AllBeEquivalentTo(1, {0})",
			"Expect.That(subject).All().AreEquivalentTo(1)");
		theoryData.AddWithBecause("string[] subject = [\"1\", \"2\",];",
			"subject.Should().AllBeEquivalentTo(\"2\", {0})",
			"Expect.That(subject).All().AreEqualTo(\"2\")");
		theoryData.AddWithBecause("string[] subject = [\"1\", \"2\",];",
			"subject.Should().AllBeEquivalentTo(\"2\", o => o.IgnoringCase(), {0})",
			"Expect.That(subject).All().AreEqualTo(\"2\").IgnoringCase()");
		theoryData.AddWithBecause("string[] subject = [\"1\", \"2\",];",
			"subject.Should().AllBeEquivalentTo(\"2\", o => o.IgnoringLeadingWhitespace(), {0})",
			"Expect.That(subject).All().AreEqualTo(\"2\").IgnoringLeadingWhiteSpace()");
		theoryData.AddWithBecause("IEnumerable<string> subject = [\"1\", \"2\",];",
			"subject.Should().AllBeEquivalentTo(\"2\", o => o.IgnoringTrailingWhitespace(), {0})",
			"Expect.That(subject).All().AreEqualTo(\"2\").IgnoringTrailingWhiteSpace()");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().AllBeAssignableTo<ArgumentException>({0})",
			"Expect.That(subject).All().Are<ArgumentException>()");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().AllBeAssignableTo(typeof(ArgumentException), {0})",
			"Expect.That(subject).All().Are(typeof(ArgumentException))");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().AllBeOfType<ArgumentException>({0})",
			"Expect.That(subject).All().AreExactly<ArgumentException>()");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().AllBeOfType(typeof(ArgumentException), {0})",
			"Expect.That(subject).All().AreExactly(typeof(ArgumentException))");
		theoryData.AddWithBecause("int[] subject = [1, 2,];int[] expected = [1, 2,];",
			"subject.Should().BeSubsetOf(expected, {0})",
			"Expect.That(subject).IsContainedIn(expected).InAnyOrder()");
		theoryData.AddWithBecause("int[] subject = [1, 2,];int[] expected = [1, 2,];",
			"subject.Should().NotBeSubsetOf(expected, {0})",
			"Expect.That(subject).IsNotContainedIn(expected).InAnyOrder()");
		theoryData.AddWithBecause("int[] subject = [1, 2,];int[] expected = [1, 2,];",
			"subject.Should().ContainInOrder(expected, {0})",
			"Expect.That(subject).Contains(expected).IgnoringInterspersedItems()");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().ContainInOrder(1, 2, 3)",
			"Expect.That(subject).Contains([1, 2, 3]).IgnoringInterspersedItems()");
		theoryData.AddWithBecause("int[] subject = [1, 2,];int[] expected = [1, 2,];",
			"subject.Should().ContainInConsecutiveOrder(expected, {0})",
			"Expect.That(subject).Contains(expected)");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().ContainInConsecutiveOrder(1, 2, 3)",
			"Expect.That(subject).Contains([1, 2, 3])");
		theoryData.AddWithBecause("int[] subject = [1, 2,];int[] expected = [1, 2,];",
			"subject.Should().Contain(expected, {0})",
			"Expect.That(subject).Contains(expected).InAnyOrder().IgnoringInterspersedItems()");
		theoryData.AddWithBecause("object[] subject = [1, 2,];object expected = new();",
			"subject.Should().ContainEquivalentOf(expected, {0})",
			"Expect.That(subject).Contains(expected).Equivalent()");
		theoryData.AddWithBecause("object[] subject = [1, 2,];object expected = new();",
			"subject.Should().NotContainEquivalentOf(expected, {0})",
			"Expect.That(subject).DoesNotContain(expected).Equivalent()");
		theoryData.AddWithBecause("int[] subject = [1, 2,];int[] expected = [1, 2,];",
			"subject.Should().StartWith(expected, {0})",
			"Expect.That(subject).StartsWith(expected)");
		theoryData.AddWithBecause("int[] subject = [1, 2,];int[] expected = [1, 2,];",
			"subject.Should().EndWith(expected, {0})",
			"Expect.That(subject).EndsWith(expected)");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().BeInAscendingOrder({0})",
			"Expect.That(subject).IsInAscendingOrder()");
		theoryData.AddWithBecause("string[] subject = [\"a\",\"b\"];",
			"subject.Should().BeInAscendingOrder(StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsInAscendingOrder().Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().BeInAscendingOrder(x => x.GetHashCode(), {0})",
			"Expect.That(subject).IsInAscendingOrder(x => x.GetHashCode())");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().BeInAscendingOrder(x => x.ToString(), StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsInAscendingOrder(x => x.ToString()).Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().NotBeInAscendingOrder({0})",
			"Expect.That(subject).IsNotInAscendingOrder()");
		theoryData.AddWithBecause("string[] subject = [\"a\",\"b\"];",
			"subject.Should().NotBeInAscendingOrder(StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsNotInAscendingOrder().Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().NotBeInAscendingOrder(x => x.ToString(), StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsNotInAscendingOrder(x => x.ToString()).Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().NotBeInAscendingOrder(x => x.GetHashCode(), {0})",
			"Expect.That(subject).IsNotInAscendingOrder(x => x.GetHashCode())");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().BeInDescendingOrder({0})",
			"Expect.That(subject).IsInDescendingOrder()");
		theoryData.AddWithBecause("string[] subject = [\"a\",\"b\"];",
			"subject.Should().BeInDescendingOrder(StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsInDescendingOrder().Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().BeInDescendingOrder(x => x.GetHashCode(), {0})",
			"Expect.That(subject).IsInDescendingOrder(x => x.GetHashCode())");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().BeInDescendingOrder(x => x.ToString(), StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsInDescendingOrder(x => x.ToString()).Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("int[] subject = [1, 2,];",
			"subject.Should().NotBeInDescendingOrder({0})",
			"Expect.That(subject).IsNotInDescendingOrder()");
		theoryData.AddWithBecause("string[] subject = [\"a\",\"b\"];",
			"subject.Should().NotBeInDescendingOrder(StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsNotInDescendingOrder().Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().NotBeInDescendingOrder(x => x.GetHashCode(), {0})",
			"Expect.That(subject).IsNotInDescendingOrder(x => x.GetHashCode())");
		theoryData.AddWithBecause("object[] subject = [];",
			"subject.Should().NotBeInDescendingOrder(x => x.ToString(), StringComparer.Ordinal, {0})",
			"Expect.That(subject).IsNotInDescendingOrder(x => x.ToString()).Using(StringComparer.Ordinal)");
		theoryData.AddWithBecause("int[] subject = [1, 2, 3, 1, ];",
			"subject.Should().OnlyHaveUniqueItems({0})",
			"Expect.That(subject).AreAllUnique()");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/collections/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Enums()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("DayOfWeek subject = DayOfWeek.Monday;",
			"subject.Should().BeDefined({0})",
			"Expect.That(subject).IsDefined()");
		theoryData.AddWithBecause("DayOfWeek subject = DayOfWeek.Monday;",
			"subject.Should().NotBeDefined({0})",
			"Expect.That(subject).IsNotDefined()");
		theoryData.AddWithBecause("DayOfWeek? subject = DayOfWeek.Monday;",
			"subject.Should().HaveValue({0})",
			"Expect.That(subject).IsNotNull()");
		theoryData.AddWithBecause("DayOfWeek? subject = DayOfWeek.Monday;",
			"subject.Should().HaveValue(1, {0})",
			"Expect.That(subject).HasValue(1)");
		theoryData.AddWithBecause("DayOfWeek? subject = DayOfWeek.Monday;",
			"subject.Should().NotHaveValue({0})",
			"Expect.That(subject).IsNull()");
		theoryData.AddWithBecause("DayOfWeek? subject = DayOfWeek.Monday;",
			"subject.Should().NotHaveValue(2, {0})",
			"Expect.That(subject).DoesNotHaveValue(2)");
		theoryData.AddWithBecause("DayOfWeek subject = DayOfWeek.Monday;",
			"subject.Should().HaveFlag(DayOfWeek.Tuesday, {0})",
			"Expect.That(subject).HasFlag(DayOfWeek.Tuesday)");
		theoryData.AddWithBecause("DayOfWeek subject = DayOfWeek.Monday;",
			"subject.Should().NotHaveFlag(DayOfWeek.Tuesday, {0})",
			"Expect.That(subject).DoesNotHaveFlag(DayOfWeek.Tuesday)");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/exceptions/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Equivalency()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("object subject = new object();object expected = new object();",
			"subject.Should().BeEquivalentTo(expected, {0})",
			"Expect.That(subject).IsEquivalentTo(expected)");
		theoryData.AddWithBecause("byte[] subject = [];byte[] expected = [];",
			"subject.Should().BeEquivalentTo(expected, {0})",
			"Expect.That(subject).IsEqualTo(expected).InAnyOrder()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"foo\";",
			"subject.Should().BeEquivalentTo(expected, {0})",
			"Expect.That(subject).IsEqualTo(expected).IgnoringCase()");
		theoryData.AddWithBecause("IEnumerable<string> subject = [];string[] expected = [];",
			"subject.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering(), {0})",
			"Expect.That(subject).IsEqualTo(expected)");
		theoryData.AddWithBecause("AggregateException subject = new(); Exception[] expected = [];",
			"subject.InnerExceptions.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering(), {0})",
			"Expect.That(subject.InnerExceptions).IsEqualTo(expected)");
		theoryData.AddWithBecause("int[] subject = [];int[] expected = [];",
			"subject.Should().BeEquivalentTo(expected, o => o.WithoutStrictOrdering(), {0})",
			"Expect.That(subject).IsEqualTo(expected).InAnyOrder()");
		theoryData.AddWithBecause("object subject = new object();object unexpected = new object();",
			"subject.Should().NotBeEquivalentTo(unexpected, {0})",
			"Expect.That(subject).IsNotEquivalentTo(unexpected)");
		theoryData.AddWithBecause("int[] subject = [];int[] unexpected = [];",
			"subject.Should().NotBeEquivalentTo(unexpected, {0})",
			"Expect.That(subject).IsNotEqualTo(unexpected).InAnyOrder()");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/exceptions/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Exceptions()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("Action callback = () => {};",
			"callback.Should().NotThrow({0})",
			"Expect.That(callback).DoesNotThrow()");
		theoryData.AddWithBecause("Action callback = () => {};",
			"callback.Should().NotThrow<ArgumentException>({0})",
			"Expect.That(callback).DoesNotThrow<ArgumentException>()");
		theoryData.AddWithBecause("Func<Task> callback = () => Task.CompletedTask;",
			"callback.Should().NotThrowAsync({0})",
			"Expect.That(callback).DoesNotThrow()",
			true);
		theoryData.AddWithBecause("Func<Task> callback = () => Task.CompletedTask;",
			"callback.Should().NotThrowAsync<ArgumentException>({0})",
			"Expect.That(callback).DoesNotThrow<ArgumentException>()",
			true);
		theoryData.AddWithBecause("Action callback = () => {};",
			"callback.Should().Throw<ArgumentException>({0})",
			"Expect.That(callback).Throws<ArgumentException>()");
		theoryData.AddWithBecause("Action callback = () => {};",
			"callback.Should().ThrowExactly<ArgumentException>({0})",
			"Expect.That(callback).ThrowsExactly<ArgumentException>()");
		theoryData.AddWithBecause("Func<Task> callback = () => Task.CompletedTask;",
			"callback.Should().ThrowAsync<ArgumentException>({0})",
			"Expect.That(callback).Throws<ArgumentException>()",
			true);
		theoryData.AddWithBecause("Func<Task> callback = () => Task.CompletedTask;",
			"callback.Should().ThrowExactlyAsync<ArgumentException>({0})",
			"Expect.That(callback).ThrowsExactly<ArgumentException>()",
			true);
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/numerictypes/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Numbers()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("int subject = 1;",
			"subject.Should().BePositive({0})",
			"Expect.That(subject).IsPositive()");
		theoryData.AddWithBecause("int subject = 1;",
			"subject.Should().BeNegative({0})",
			"Expect.That(subject).IsNegative()");
		theoryData.AddWithBecause("int subject = 1;int expected = 2;",
			"subject.Should().BeGreaterThan(expected, {0})",
			"Expect.That(subject).IsGreaterThan(expected)");
		theoryData.AddWithBecause("int subject = 1;int expected = 2;",
			"subject.Should().BeGreaterThanOrEqualTo(expected, {0})",
			"Expect.That(subject).IsGreaterThanOrEqualTo(expected)");
		theoryData.AddWithBecause("int subject = 1;int expected = 2;",
			"subject.Should().BeLessThan(expected, {0})",
			"Expect.That(subject).IsLessThan(expected)");
		theoryData.AddWithBecause("int subject = 1;int expected = 2;",
			"subject.Should().BeLessThanOrEqualTo(expected, {0})",
			"Expect.That(subject).IsLessThanOrEqualTo(expected)");
		theoryData.AddWithBecause("double subject = 1.1;double expected = 1.0;double tolerance = 0.05;",
			"subject.Should().BeApproximately(expected, tolerance, {0})",
			"Expect.That(subject).IsEqualTo(expected).Within(tolerance)");
		theoryData.AddWithBecause("double subject = 1.1;double expected = 1.0;double tolerance = 0.05;",
			"subject.Should().NotBeApproximately(expected, tolerance, {0})",
			"Expect.That(subject).IsNotEqualTo(expected).Within(tolerance)");
		theoryData.AddWithBecause("uint subject = 1;uint expected = 2;uint delta = 3;",
			"subject.Should().BeCloseTo(expected, delta, {0})",
			"Expect.That(subject).IsEqualTo(expected).Within(delta)");
		theoryData.AddWithBecause("byte subject = 1;byte expected = 2;byte delta = 3;",
			"subject.Should().BeCloseTo(expected, delta, {0})",
			"Expect.That(subject).IsEqualTo(expected).Within(delta)");
		theoryData.AddWithBecause("uint subject = 1;uint expected = 2;uint delta = 3;",
			"subject.Should().NotBeCloseTo(expected, delta, {0})",
			"Expect.That(subject).IsNotEqualTo(expected).Within(delta)");
		theoryData.AddWithBecause("byte subject = 1;byte expected = 2;byte delta = 3;",
			"subject.Should().NotBeCloseTo(expected, delta, {0})",
			"Expect.That(subject).IsNotEqualTo(expected).Within(delta)");
		theoryData.AddWithBecause("int subject = 1;int[] expected = [2, 3,];",
			"subject.Should().BeOneOf(expected, {0})",
			"Expect.That(subject).IsOneOf(expected)");
		theoryData.AddWithBecause("int subject = 1;",
			"subject.Should().BeOneOf(2, 3, 4)",
			"Expect.That(subject).IsOneOf(2, 3, 4)");
		theoryData.AddWithBecause("int subject = 1;",
			"subject.Should().BeInRange(0, 2, {0})",
			"Expect.That(subject).IsBetween(0).And(2)");
		theoryData.AddWithBecause("int subject = 1;",
			"subject.Should().NotBeInRange(2, 3, {0})",
			"Expect.That(subject).IsNotBetween(2).And(3)");
		return theoryData;
	}

	/// <summary>
	///     Legacy expectations on FluentAssertions 7.2.x
	/// </summary>
	public static TheoryData<string, string, string, bool> Legacy()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("int subject = 1;int expected = 2;",
			"subject.Should().BeLessOrEqualTo(expected, {0})",
			"Expect.That(subject).IsLessThanOrEqualTo(expected)");
		theoryData.AddWithBecause("int subject = 1;int expected = 2;",
			"subject.Should().BeGreaterOrEqualTo(expected, {0})",
			"Expect.That(subject).IsGreaterThanOrEqualTo(expected)");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/datetimespans/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Chronology()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime expected = DateTime.Now;",
			"subject.Should().BeAfter(expected, {0})",
			"Expect.That(subject).IsAfter(expected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime expected = DateTime.Now;",
			"subject.Should().BeOnOrAfter(expected, {0})",
			"Expect.That(subject).IsOnOrAfter(expected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime expected = DateTime.Now;",
			"subject.Should().BeBefore(expected, {0})",
			"Expect.That(subject).IsBefore(expected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime expected = DateTime.Now;",
			"subject.Should().BeOnOrBefore(expected, {0})",
			"Expect.That(subject).IsOnOrBefore(expected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime unexpected = DateTime.Now;",
			"subject.Should().NotBeAfter(unexpected, {0})",
			"Expect.That(subject).IsNotAfter(unexpected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime unexpected = DateTime.Now;",
			"subject.Should().NotBeOnOrAfter(unexpected, {0})",
			"Expect.That(subject).IsNotOnOrAfter(unexpected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime unexpected = DateTime.Now;",
			"subject.Should().NotBeBefore(unexpected, {0})",
			"Expect.That(subject).IsNotBefore(unexpected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime unexpected = DateTime.Now;",
			"subject.Should().NotBeOnOrBefore(unexpected, {0})",
			"Expect.That(subject).IsNotOnOrBefore(unexpected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;DateTime[] expected = [DateTime.Now,];",
			"subject.Should().BeOneOf(expected, {0})",
			"Expect.That(subject).IsOneOf(expected)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().HaveYear(5, {0})",
			"Expect.That(subject).HasYear().EqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().HaveMonth(5, {0})",
			"Expect.That(subject).HasMonth().EqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().HaveDay(5, {0})",
			"Expect.That(subject).HasDay().EqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().HaveHour(5, {0})",
			"Expect.That(subject).HasHour().EqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().HaveMinute(5, {0})",
			"Expect.That(subject).HasMinute().EqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().HaveSecond(5, {0})",
			"Expect.That(subject).HasSecond().EqualTo(5)");
		theoryData.AddWithBecause("DateTimeOffset subject = DateTimeOffset.Now;",
			"subject.Should().HaveOffset(TimeSpan.FromHours(4), {0})",
			"Expect.That(subject).HasOffset().EqualTo(TimeSpan.FromHours(4))");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().NotHaveYear(5, {0})",
			"Expect.That(subject).HasYear().NotEqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().NotHaveMonth(5, {0})",
			"Expect.That(subject).HasMonth().NotEqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().NotHaveDay(5, {0})",
			"Expect.That(subject).HasDay().NotEqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().NotHaveHour(5, {0})",
			"Expect.That(subject).HasHour().NotEqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().NotHaveMinute(5, {0})",
			"Expect.That(subject).HasMinute().NotEqualTo(5)");
		theoryData.AddWithBecause("DateTime subject = DateTime.Now;",
			"subject.Should().NotHaveSecond(5, {0})",
			"Expect.That(subject).HasSecond().NotEqualTo(5)");
		theoryData.AddWithBecause("DateTimeOffset subject = DateTimeOffset.Now;",
			"subject.Should().NotHaveOffset(TimeSpan.FromHours(4), {0})",
			"Expect.That(subject).HasOffset().NotEqualTo(TimeSpan.FromHours(4))");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://fluentassertions.com/strings/" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Strings()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().BeNull({0})",
			"Expect.That(subject).IsNull()");
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().NotBeNull({0})",
			"Expect.That(subject).IsNotNull()");
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().BeEmpty({0})",
			"Expect.That(subject).IsEmpty()");
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().NotBeEmpty({0})",
			"Expect.That(subject).IsNotEmpty()");
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().BeNullOrEmpty({0})",
			"Expect.That(subject).IsNullOrEmpty()");
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().NotBeNullOrEmpty({0})",
			"Expect.That(subject).IsNotNullOrEmpty()");
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().BeNullOrWhiteSpace({0})",
			"Expect.That(subject).IsNullOrWhiteSpace()");
		theoryData.AddWithBecause("string subject = \"foo\";",
			"subject.Should().NotBeNullOrWhiteSpace({0})",
			"Expect.That(subject).IsNotNullOrWhiteSpace()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Be(expected, {0})",
			"Expect.That(subject).IsEqualTo(expected)");
		theoryData.AddWithBecause("string subject = \"foo\";string unexpected = \"bar\";",
			"subject.Should().NotBe(unexpected, {0})",
			"Expect.That(subject).IsNotEqualTo(unexpected)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, {0})",
			"Expect.That(subject).Contains(expected)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, AtLeast.Once(), {0})",
			"Expect.That(subject).Contains(expected).AtLeast().Once()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, AtLeast.Twice(), {0})",
			"Expect.That(subject).Contains(expected).AtLeast().Twice()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, AtLeast.Thrice(), {0})",
			"Expect.That(subject).Contains(expected).AtLeast(3.Times())");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, AtLeast.Times(4), {0})",
			"Expect.That(subject).Contains(expected).AtLeast(4)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, AtMost.Once(), {0})",
			"Expect.That(subject).Contains(expected).AtMost().Once()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, AtMost.Twice(), {0})",
			"Expect.That(subject).Contains(expected).AtMost().Twice()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, AtMost.Thrice(), {0})",
			"Expect.That(subject).Contains(expected).AtMost(3.Times())");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";int expectedCount=5;",
			"subject.Should().Contain(expected, AtMost.Times(expectedCount), {0})",
			"Expect.That(subject).Contains(expected).AtMost(expectedCount)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, Exactly.Once(), {0})",
			"Expect.That(subject).Contains(expected).Once()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, Exactly.Twice(), {0})",
			"Expect.That(subject).Contains(expected).Twice()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, Exactly.Thrice(), {0})",
			"Expect.That(subject).Contains(expected).Exactly(3.Times())");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, Exactly.Times(4), {0})",
			"Expect.That(subject).Contains(expected).Exactly(4)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, LessThan.Twice(), {0})",
			"Expect.That(subject).Contains(expected).LessThan().Twice()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, LessThan.Thrice(), {0})",
			"Expect.That(subject).Contains(expected).LessThan(3.Times())");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, LessThan.Times(4), {0})",
			"Expect.That(subject).Contains(expected).LessThan(4)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, MoreThan.Once(), {0})",
			"Expect.That(subject).Contains(expected).MoreThan().Once()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, MoreThan.Twice(), {0})",
			"Expect.That(subject).Contains(expected).MoreThan().Twice()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, MoreThan.Thrice(), {0})",
			"Expect.That(subject).Contains(expected).MoreThan(3.Times())");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().Contain(expected, MoreThan.Times(4), {0})",
			"Expect.That(subject).Contains(expected).MoreThan(4)");
		theoryData.AddWithBecause("string subject = \"foo\";string unexpected = \"bar\";",
			"subject.Should().NotContain(unexpected, {0})",
			"Expect.That(subject).DoesNotContain(unexpected)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().StartWith(expected, {0})",
			"Expect.That(subject).StartsWith(expected)");
		theoryData.AddWithBecause("string subject = \"foo\";string unexpected = \"bar\";",
			"subject.Should().NotStartWith(unexpected, {0})",
			"Expect.That(subject).DoesNotStartWith(unexpected)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().EndWith(expected, {0})",
			"Expect.That(subject).EndsWith(expected)");
		theoryData.AddWithBecause("string subject = \"foo\";string unexpected = \"bar\";",
			"subject.Should().NotEndWith(unexpected, {0})",
			"Expect.That(subject).DoesNotEndWith(unexpected)");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().BeEquivalentTo(expected, {0})",
			"Expect.That(subject).IsEqualTo(expected).IgnoringCase()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().BeEquivalentTo(expected, o => o.IgnoringCase(), {0})",
			"Expect.That(subject).IsEqualTo(expected).IgnoringCase()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().BeEquivalentTo(expected, o => o.IgnoringLeadingWhitespace(), {0})",
			"Expect.That(subject).IsEqualTo(expected).IgnoringCase().IgnoringLeadingWhiteSpace()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().BeEquivalentTo(expected, o => o.IgnoringTrailingWhitespace(), {0})",
			"Expect.That(subject).IsEqualTo(expected).IgnoringCase().IgnoringTrailingWhiteSpace()");
		theoryData.AddWithBecause("string subject = \"foo\";string expected = \"bar\";",
			"subject.Should().BeEquivalentTo(expected, o => o.IgnoringNewlineStyle(), {0})",
			"Expect.That(subject).IsEqualTo(expected).IgnoringCase().IgnoringNewlineStyle()");
		theoryData.AddWithBecause("string subject = \"foo\"; string expected = \"bar\";",
			"subject.Should().Match(expected, {0})",
			"Expect.That(subject).IsEqualTo(expected).AsWildcard()");
		return theoryData;
	}

	private static void AddWithBecause(this TheoryData<string, string, string, bool> theoryData,
		string arrange,
		string actual,
		string expected,
		bool isAsync = false)
	{
		if (actual.Contains("({0})"))
		{
			theoryData.Add(
				string.Format(actual, ""),
				expected,
				arrange, isAsync);
			theoryData.Add(
				string.Format(actual, "\"because foo\""),
				expected + ".Because(\"because foo\")",
				arrange, isAsync);
		}
		else if (actual.Contains(", {0})") || actual.Contains(",{0})"))
		{
			theoryData.Add(
				actual.Replace(", {0})", ")").Replace(",{0})", ")"),
				expected,
				arrange, isAsync);
			theoryData.Add(
				string.Format(actual, "\"because foo\""),
				expected + ".Because(\"because foo\")",
				arrange, isAsync);
		}
		else
		{
			theoryData.Add(
				actual,
				expected,
				arrange, isAsync);
		}
	}
}
