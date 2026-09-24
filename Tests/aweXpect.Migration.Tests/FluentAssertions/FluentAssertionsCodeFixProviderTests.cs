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

	[Theory]
	[MemberData(nameof(TestCases.Basic), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForBasicTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Boolean), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForBooleanTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Chronology), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForChronologyTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Collection), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForCollectionTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Enums), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForEnumsTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Equivalency), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForEquivalencyTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Exceptions), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForExceptionsTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Legacy), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForLegacyTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyLegacyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Numbers), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForNumbersTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Strings), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForStringsTestCases(
		string fluentAssertions,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(fluentAssertions, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.NoCodeFix), MemberType = typeof(TestCases))]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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


	[Fact]
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

	[Fact]
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
