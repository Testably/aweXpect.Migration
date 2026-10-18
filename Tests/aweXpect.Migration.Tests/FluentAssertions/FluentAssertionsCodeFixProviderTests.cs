using Verifier =
	aweXpect.Migration.Tests.Verifiers.CSharpCodeFixVerifier<aweXpect.Migration.Analyzers.FluentAssertionsAnalyzer,
		aweXpect.Migration.Analyzers.FluentAssertionsCodeFixProvider>;

namespace aweXpect.Migration.Tests.FluentAssertions;

public class FluentAssertionsCodeFixProviderTests
{
	/// <summary>
	///     The line ending of the raw string literals, which depends on how the file was checked out.
	/// </summary>
	private static readonly string SourceNewLine = """
	                                               a
	                                               b
	                                               """.Contains('\r') ? "\r\n" : "\n";

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Basic))]
	public async Task ShouldApplyCodeFixForBasicTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Boolean))]
	public async Task ShouldApplyCodeFixForBooleanTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Chronology))]
	public async Task ShouldApplyCodeFixForChronologyTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Collection))]
	public async Task ShouldApplyCodeFixForCollectionTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Enums))]
	public async Task ShouldApplyCodeFixForEnumsTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Equivalency))]
	public async Task ShouldApplyCodeFixForEquivalencyTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Exceptions))]
	public async Task ShouldApplyCodeFixForExceptionsTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Legacy))]
	public async Task ShouldApplyCodeFixForLegacyTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyLegacyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Numbers))]
	public async Task ShouldApplyCodeFixForNumbersTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Strings))]
	public async Task ShouldApplyCodeFixForStringsTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.NoCodeFix))]
	public async Task ShouldNotOfferCodeFix(string arrange, string fluentAssertions)
	{
		string source = $$"""
		                  using System;
		                  using System.Collections.Generic;
		                  using System.Linq;
		                  using System.Threading.Tasks;
		                  using aweXpect;
		                  using FluentAssertions;
		                  using Xunit;

		                  public class MyClass
		                  {
		                      [Fact]
		                      public void MyTest()
		                      {
		                          {{arrange}}

		                          [|{{fluentAssertions}}|];
		                      }
		                  }
		                  """;

		await Verifier.VerifyCodeFixAsync(source, source);
	}

	[Test]
	public async Task IgnoringCollectionOrder_ShouldAddUsingOnceForAllFixes() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using System;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        object subject = new();

			        [|subject.Should().BeEquivalentTo(new { Value = 1 })|];
			        [|subject.Should().NotBeEquivalentTo(new { Value = 2 })|];
			    }
			}
			""",
			"""
			using System;
			using aweXpect;
			using aweXpect.Equivalency;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        object subject = new();

			        Expect.That(subject).IsEquivalentTo(new { Value = 1 }, o => o.IgnoringCollectionOrder());
			        Expect.That(subject).IsNotEquivalentTo(new { Value = 2 }, o => o.IgnoringCollectionOrder());
			    }
			}
			"""
		);

	[Test]
	public async Task IgnoringCollectionOrder_WhenNamespaceIsImported_ShouldNotAddUsing() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			namespace MyNamespace
			{
			    using aweXpect.Equivalency;

			    public class MyClass
			    {
			        [Fact]
			        public void MyTest()
			        {
			            object subject = new();

			            [|subject.Should().BeEquivalentTo(new { Value = 1 })|];
			        }
			    }
			}
			""",
			"""
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			namespace MyNamespace
			{
			    using aweXpect.Equivalency;

			    public class MyClass
			    {
			        [Fact]
			        public void MyTest()
			        {
			            object subject = new();

			            Expect.That(subject).IsEquivalentTo(new { Value = 1 }, o => o.IgnoringCollectionOrder());
			        }
			    }
			}
			"""
		);

	[Test]
	public async Task IgnoringCollectionOrder_ShouldAddUsingAfterGlobalUsingsAndKeepHeader() => await Verifier
		.VerifyCodeFixAsync(
			"""
			// header
			global using Xunit;
			using FluentAssertions;
			using aweXpect;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        object subject = new();

			        [|subject.Should().BeEquivalentTo(new { Value = 1 })|];
			    }
			}
			""",
			"""
			// header
			global using Xunit;
			using aweXpect.Equivalency;
			using FluentAssertions;
			using aweXpect;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        object subject = new();

			        Expect.That(subject).IsEquivalentTo(new { Value = 1 }, o => o.IgnoringCollectionOrder());
			    }
			}
			"""
		);

	[Test]
	public async Task ChainContinuingOnWhich_ShouldNotOfferCodeFix()
	{
		const string source = """
		                      using System;
		                      using aweXpect;
		                      using FluentAssertions;
		                      using Xunit;

		                      public class MyClass
		                      {
		                          [Fact]
		                          public void MyTest()
		                          {
		                              Action callback = () => {};

		                              {|#0:callback.Should().Throw<ArgumentException>().Which.Message.Should().Be("foo")|};
		                          }
		                      }
		                      """;

		await Verifier.VerifyCodeFixAsync(source,
			[Verifier.Diagnostic().WithLocation(0), Verifier.Diagnostic().WithLocation(0),], source);
	}

	[Test]
	public async Task ThrowsWithMessageAndBecause_ShouldKeepBecause() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using System;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        Action callback = () => {};

			        [|callback.Should().Throw<ArgumentException>().WithMessage("foo*", "because {0}", 1)|];
			    }
			}
			""",
			"""
			using System;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        Action callback = () => {};

			        Expect.That(callback).Throws<ArgumentException>().WithMessage("foo*").AsWildcard().IgnoringCase().IgnoringNewlineStyle().Because($"because {1}");
			    }
			}
			"""
		);

	[Test]
	public async Task ShouldApplyCodeFixInTheory() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Theory]
			    [InlineData("bar")]
			    public void MyTest(string expected)
			    {
			        string subject = "foo";
			        
			        [|subject.Should().Be(expected)|];
			    }
			}
			""",
			"""
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Theory]
			    [InlineData("bar")]
			    public void MyTest(string expected)
			    {
			        string subject = "foo";
			        
			        Expect.That(subject).IsEqualTo(expected);
			    }
			}
			"""
		);

	[Test]
	public async Task ShouldSupportActions() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using System;
			using System.Threading.Tasks;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyTestClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        Action action = [|() => true.Should().BeTrue()|];
			        
			        action();
			    }
			}
			""",
			"""
			using System;
			using System.Threading.Tasks;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyTestClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        Action action = () => aweXpect.Synchronous.Synchronously.Verify(Expect.That(true).IsTrue());
			        
			        action();
			    }
			}
			"""
		);

	[Test]
	public async Task ShouldSupportBecauseWithParameters() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using System;
			using System.Threading.Tasks;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyTestClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        [|true.Should().BeTrue("Because with {0} {1}", 2, "parameters")|];
			    }
			}
			""",
			"""
			using System;
			using System.Threading.Tasks;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyTestClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        Expect.That(true).IsTrue().Because($"Because with {2} {"parameters"}");
			    }
			}
			"""
		);


	[Test]
	public async Task ShouldSupportNullablePropertyAccess() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using System;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyTestClass
			{
			    [Theory]
			    [InlineData("bar")]
			    public void MyTest(string expected)
			    {
			        MyClass subject = new();
			        
			        [|subject?.Inner?.NewInner()?.Value.Should().Be(expected)|];
			    }
			    private class MyClass
			    {
			        public MyClass NewInner() => new MyClass();
			        public MyClass Inner => null;
			        public string Value => "";
			    }
			}
			""",
			"""
			using System;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyTestClass
			{
			    [Theory]
			    [InlineData("bar")]
			    public void MyTest(string expected)
			    {
			        MyClass subject = new();
			        
			        Expect.That(subject?.Inner?.NewInner()?.Value).IsEqualTo(expected);
			    }
			    private class MyClass
			    {
			        public MyClass NewInner() => new MyClass();
			        public MyClass Inner => null;
			        public string Value => "";
			    }
			}
			"""
		);

	[Test]
	public async Task ThrowsWithMessage_ShouldApplyCorrectCodeFix() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using System;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Theory]
			    [InlineData("bar")]
			    public void MyTest(string expected)
			    {
			        Action callback = () => {};
			        
			        [|callback.Should().Throw<ArgumentException>().WithMessage("foo*")|];
			    }
			}
			""",
			"""
			using System;
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Theory]
			    [InlineData("bar")]
			    public void MyTest(string expected)
			    {
			        Action callback = () => {};
			        
			        Expect.That(callback).Throws<ArgumentException>().WithMessage("foo*").AsWildcard().IgnoringCase().IgnoringNewlineStyle();
			    }
			}
			"""
		);

	[Test]
	[Arguments("subject.Should().BeEquivalentTo(expected)",
		"Expect.That(subject).IsEqualTo(expected)")]
	[Arguments("subject.Should().NotBeEquivalentTo(expected)",
		"Expect.That(subject).IsNotEqualTo(expected)")]
	[Arguments("subjects.Should().ContainEquivalentOf(expected)",
		"Expect.That(subjects).Contains(expected)")]
	[Arguments("subjects.Should().NotContainEquivalentOf(expected)",
		"Expect.That(subjects).DoesNotContain(expected)")]
	[Arguments("subjects.Should().AllBeEquivalentTo(expected)",
		"Expect.That(subjects).All().AreEqualTo(expected)")]
	public async Task Equivalency_WhenTypeOverridesEquals_ShouldCompareWithEquals(
		string fluentAssertions, string aweXpect) => await Verifier
		.VerifyCodeFixAsync(
			$$"""
			  using aweXpect;
			  using FluentAssertions;
			  using Xunit;

			  public class MyClass
			  {
			      [Fact]
			      public void MyTest()
			      {
			          Money subject = new(1, "EUR");
			          Money[] subjects = [subject,];
			          Money expected = new(1, "EUR");

			          [|{{fluentAssertions}}|];
			      }

			      private class Money(int amount, string currency)
			      {
			          public int Amount { get; } = amount;
			          public string Currency { get; } = currency;

			          public override bool Equals(object obj)
			              => obj is Money other && other.Amount == Amount && other.Currency == Currency;

			          public override int GetHashCode() => Amount;
			      }
			  }
			  """,
			$$"""
			  using aweXpect;
			  using FluentAssertions;
			  using Xunit;

			  public class MyClass
			  {
			      [Fact]
			      public void MyTest()
			      {
			          Money subject = new(1, "EUR");
			          Money[] subjects = [subject,];
			          Money expected = new(1, "EUR");

			          {{aweXpect}};
			      }

			      private class Money(int amount, string currency)
			      {
			          public int Amount { get; } = amount;
			          public string Currency { get; } = currency;

			          public override bool Equals(object obj)
			              => obj is Money other && other.Amount == Amount && other.Currency == Currency;

			          public override int GetHashCode() => Amount;
			      }
			  }
			  """
		);

	private static async Task VerifyTestCase(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await Verifier
		.VerifyCodeFixAsync(
			$$"""
			  using System;
			  using System.Collections.Generic;
			  using System.Linq;
			  using System.Threading.Tasks;
			  using aweXpect;
			  using aweXpect.Core;
			  using FluentAssertions;
			  using Xunit;

			  public class MyClass
			  {
			      [Fact]
			      public {{(isAsync ? "async Task" : "void")}} MyTest()
			      {
			          {{arrange}}
			          
			          {{(isAsync ? "await " : "")}}[|{{fluentAssertions}}|];
			      }
			  }
			  """,
			$$"""
			  using System;
			  using System.Collections.Generic;
			  using System.Linq;
			  using System.Threading.Tasks;
			  using aweXpect;
			  using aweXpect.Core;
			  {{(aweXpect.Contains(".IgnoringCollectionOrder()") ? "using aweXpect.Equivalency;" + SourceNewLine : "")}}using FluentAssertions;
			  using Xunit;

			  public class MyClass
			  {
			      [Fact]
			      public {{(isAsync ? "async Task" : "void")}} MyTest()
			      {
			          {{arrange}}
			          
			          {{(isAsync ? "await " : "")}}{{aweXpect}};
			      }
			  }
			  """
		);

	private static async Task VerifyLegacyTestCase(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await Verifier
		.VerifyLegacyCodeFixAsync(
			$$"""
			  using System;
			  using System.Collections.Generic;
			  using System.Linq;
			  using System.Threading.Tasks;
			  using aweXpect;
			  using aweXpect.Core;
			  using FluentAssertions;
			  using Xunit;

			  public class MyClass
			  {
			      [Fact]
			      public {{(isAsync ? "async Task" : "void")}} MyTest()
			      {
			          {{arrange}}
			          
			          {{(isAsync ? "await " : "")}}[|{{fluentAssertions}}|];
			      }
			  }
			  """,
			$$"""
			  using System;
			  using System.Collections.Generic;
			  using System.Linq;
			  using System.Threading.Tasks;
			  using aweXpect;
			  using aweXpect.Core;
			  using FluentAssertions;
			  using Xunit;

			  public class MyClass
			  {
			      [Fact]
			      public {{(isAsync ? "async Task" : "void")}} MyTest()
			      {
			          {{arrange}}
			          
			          {{(isAsync ? "await " : "")}}{{aweXpect}};
			      }
			  }
			  """
		);
}
