namespace aweXpect.Migration.Tests.Xunit;

public static class TestCases
{
	/// <summary>
	///     <see href="https://github.com/xunit/assert.xunit/tree/main" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Basic()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddTestCase("",
			"Assert.Fail(\"foo\")",
			"Fail.Test(\"foo\")");
		theoryData.AddTestCase("",
			"Assert.Skip(\"foo\")",
			"Skip.Test(\"foo\")");
		theoryData.AddTestCase("",
			"Assert.Null(new object())",
			"Expect.That(new object()).IsNull()");
		theoryData.AddTestCase("",
			"Assert.NotNull(new object())",
			"Expect.That(new object()).IsNotNull()");
		theoryData.AddTestCase("",
			"Assert.Same(new ArgumentException(), new NullReferenceException())",
			"Expect.That(new NullReferenceException()).IsSameAs(new ArgumentException())");
		theoryData.AddTestCase("",
			"Assert.NotSame(new ArgumentException(), new NullReferenceException())",
			"Expect.That(new NullReferenceException()).IsNotSameAs(new ArgumentException())");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://github.com/xunit/assert.xunit/blob/main/BooleanAsserts.cs" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Boolean()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddTestCase("bool subject = false;",
			"Assert.True(subject)",
			"Expect.That(subject).IsTrue()");
		theoryData.AddTestCase("bool subject = false;",
			"Assert.False(subject)",
			"Expect.That(subject).IsFalse()");
		theoryData.AddTestCase("bool subject = false;",
			"Assert.True(subject, \"foo\")",
			"Expect.That(subject).IsTrue().Because(\"foo\")");
		theoryData.AddTestCase("bool subject = false;",
			"Assert.False(subject, \"foo\")",
			"Expect.That(subject).IsFalse().Because(\"foo\")");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://github.com/xunit/assert.xunit/blob/main/CollectionAsserts.cs" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Collections()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddTestCase("int[] subject = [1, 2,];",
			"Assert.Distinct(subject)",
			"Expect.That(subject).All().AreUnique()");
		theoryData.AddTestCase("int[] subject = [1, 2,];",
			"Assert.Contains(1, subject)",
			"Expect.That(subject).Contains(1)");
		theoryData.AddTestCase("int[] subject = [1, 2,];",
			"Assert.Contains(subject, x => x == 1)",
			"Expect.That(subject).Contains(x => x == 1)");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://github.com/xunit/assert.xunit/blob/main/EqualityAsserts.cs" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Equality()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddTestCase("",
			"Assert.Equal(1, 2)",
			"Expect.That(2).IsEqualTo(1)");
		theoryData.AddTestCase("",
			"Assert.Equal(1.0, 1.1, 0.1)",
			"Expect.That(1.1).IsEqualTo(1.0).Within(0.1)");
		theoryData.AddTestCase("TimeSpan tolerance = TimeSpan.FromSeconds(1);",
			"Assert.Equal(DateTime.Now, DateTime.Today, tolerance)",
			"Expect.That(DateTime.Today).IsEqualTo(DateTime.Now).Within(tolerance)");
		theoryData.AddTestCase("",
			"Assert.NotEqual(1, 2)",
			"Expect.That(2).IsNotEqualTo(1)");
		theoryData.AddTestCase("",
			"Assert.NotEqual(1.0, 1.1, 0.1)",
			"Expect.That(1.1).IsNotEqualTo(1.0).Within(0.1)");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://github.com/xunit/assert.xunit/blob/main/ExceptionAsserts.cs" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Exceptions()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddTestCase("Action callback = () => {};",
			"Assert.ThrowsAny<ArgumentException>(callback)",
			"Expect.That(callback).Throws<ArgumentException>()");
		theoryData.AddTestCase("Func<Task> callback = () => Task.CompletedTask;",
			"Assert.ThrowsAnyAsync<ArgumentException>(callback)",
			"Expect.That(callback).Throws<ArgumentException>()");
		theoryData.AddTestCase("Action callback = () => {};",
			"Assert.Throws<ArgumentException>(callback)",
			"Expect.That(callback).ThrowsExactly<ArgumentException>()");
		theoryData.AddTestCase("Func<Task> callback = () => Task.CompletedTask;",
			"Assert.ThrowsAsync<ArgumentException>(callback)",
			"Expect.That(callback).ThrowsExactly<ArgumentException>()");
		theoryData.AddTestCase("Action callback = () => {};",
			"Assert.Throws(typeof(ArgumentException), callback)",
			"Expect.That(callback).ThrowsExactly(typeof(ArgumentException))");
		theoryData.AddTestCase("Func<Task> callback = () => Task.CompletedTask;",
			"Assert.ThrowsAsync(typeof(ArgumentException), callback)",
			"Expect.That(callback).ThrowsExactly(typeof(ArgumentException))");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://github.com/xunit/assert.xunit/blob/main/StringAsserts.cs" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Strings()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddTestCase("",
			"Assert.Contains(\"oo\", \"foo\")",
			"Expect.That(\"foo\").Contains(\"oo\")");
		theoryData.AddTestCase("",
			"Assert.DoesNotContain(\"oo\", \"foo\")",
			"Expect.That(\"foo\").DoesNotContain(\"oo\")");
		theoryData.AddTestCase("",
			"Assert.StartsWith(\"fo\", \"foo\")",
			"Expect.That(\"foo\").StartsWith(\"fo\")");
		theoryData.AddTestCase("",
			"Assert.EndsWith(\"oo\", \"foo\")",
			"Expect.That(\"foo\").EndsWith(\"oo\")");
		theoryData.AddTestCase("",
			"Assert.Empty(\"foo\")",
			"Expect.That(\"foo\").IsEmpty()");
		theoryData.AddTestCase("",
			"Assert.NotEmpty(\"foo\")",
			"Expect.That(\"foo\").IsNotEmpty()");
		return theoryData;
	}

	/// <summary>
	///     <see href="https://github.com/xunit/assert.xunit/blob/main/TypeAsserts.cs" />
	/// </summary>
	public static TheoryData<string, string, string, bool> Types()
	{
		TheoryData<string, string, string, bool> theoryData = new();
		theoryData.AddTestCase("",
			"Assert.IsAssignableFrom<ArgumentException>(new Exception())",
			"Expect.That(new Exception()).Is<ArgumentException>()");
		theoryData.AddTestCase("",
			"Assert.IsAssignableFrom(typeof(ArgumentException), new Exception())",
			"Expect.That(new Exception()).Is(typeof(ArgumentException))");
		theoryData.AddTestCase("",
			"Assert.IsNotAssignableFrom<ArgumentException>(new Exception())",
			"Expect.That(new Exception()).IsNot<ArgumentException>()");
		theoryData.AddTestCase("",
			"Assert.IsNotAssignableFrom(typeof(ArgumentException), new Exception())",
			"Expect.That(new Exception()).IsNot(typeof(ArgumentException))");
		theoryData.AddTestCase("",
			"Assert.IsType<ArgumentException>(new Exception())",
			"Expect.That(new Exception()).IsExactly<ArgumentException>()");
		theoryData.AddTestCase("",
			"Assert.IsType(typeof(ArgumentException), new Exception())",
			"Expect.That(new Exception()).IsExactly(typeof(ArgumentException))");
		theoryData.AddTestCase("",
			"Assert.IsNotType<ArgumentException>(new Exception())",
			"Expect.That(new Exception()).IsNotExactly<ArgumentException>()");
		theoryData.AddTestCase("",
			"Assert.IsNotType(typeof(ArgumentException), new Exception())",
			"Expect.That(new Exception()).IsNotExactly(typeof(ArgumentException))");
		return theoryData;
	}

	private static void AddTestCase(this TheoryData<string, string, string, bool> theoryData,
		string arrange,
		string actual,
		string expected,
		bool isAsync = false)
		=> theoryData.Add(
			actual,
			expected,
			arrange, isAsync);
}
