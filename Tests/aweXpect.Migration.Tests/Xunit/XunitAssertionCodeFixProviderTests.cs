using Verifier =
	aweXpect.Migration.Tests.Verifiers.CSharpCodeFixVerifier<aweXpect.Migration.Analyzers.XunitAssertionAnalyzer,
		aweXpect.Migration.Analyzers.XunitAssertionCodeFixProvider>;

namespace aweXpect.Migration.Tests.Xunit;

public class XunitAssertionCodeFixProviderTests
{
	[Theory]
	[MemberData(nameof(TestCases.Basic), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForBasicTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Boolean), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForBooleanTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Collections), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForCollectionTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Equality), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForEqualityTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Exceptions), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForExceptionTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Strings), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForStringTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Theory]
	[MemberData(nameof(TestCases.Types), MemberType = typeof(TestCases))]
	public async Task ShouldApplyCodeFixForTypeTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Fact]
	public async Task EqualWithPrecision_WithoutSystemNamespace_ShouldAddUsing() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using aweXpect;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        [|Assert.Equal(1.001, 1.002, 2)|];
			    }
			}
			""",
			"""
			using System;
			using aweXpect;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        Expect.That(Math.Round(1.002, 2)).IsEqualTo(Math.Round(1.001, 2));
			    }
			}
			"""
		);

	[Theory]
	[InlineData("Assert.Equal(\"a\", \"A\", ignoreCase: true)")]
	[InlineData("Assert.Equal(\"a\", \"A\", StringComparer.OrdinalIgnoreCase)")]
	[InlineData("Assert.Contains(\"a\", \"A\", StringComparison.OrdinalIgnoreCase)")]
	[InlineData("Assert.Throws<ArgumentException>(testCode: () => {}, paramName: \"foo\")")]
	public async Task UnsupportedArguments_ShouldNotOfferCodeFix(string xunitAssertion)
	{
		string source = $$"""
		                  using System;
		                  using aweXpect;
		                  using Xunit;

		                  public class MyClass
		                  {
		                      [Fact]
		                      public void MyTest()
		                      {
		                          [|{{xunitAssertion}}|];
		                      }
		                  }
		                  """;

		await Verifier.VerifyCodeFixAsync(source, source);
	}

	[Fact]
	public async Task IsTypeWithNonConstantExactMatch_ShouldNotOfferCodeFix()
	{
		const string source = """
		                      using System;
		                      using aweXpect;
		                      using Xunit;

		                      public class MyClass
		                      {
		                          [Theory]
		                          [InlineData(true)]
		                          public void MyTest(bool exactMatch)
		                          {
		                              [|Assert.IsType<ArgumentException>(new Exception(), exactMatch)|];
		                          }
		                      }
		                      """;

		await Verifier.VerifyCodeFixAsync(source, source);
	}

	[Fact]
	public async Task ShouldApplyCodeFixInTheory() => await Verifier
		.VerifyCodeFixAsync(
			"""
			using aweXpect;
			using Xunit;

			public class MyClass
			{
			    [Theory]
			    [InlineData("bar")]
			    public void MyTest(string expected)
			    {
			        string subject = "foo";
			        
			        [|Assert.Equal(expected, subject)|];
			    }
			}
			""",
			"""
			using aweXpect;
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


	private static async Task VerifyTestCase(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await Verifier
		.VerifyCodeFixAsync(
			$$"""
			  using System;
			  using System.Collections.Generic;
			  using System.Threading.Tasks;
			  using aweXpect;
			  using Xunit;

			  public class MyClass
			  {
			      [Fact]
			      public {{(isAsync ? "async Task" : "void")}} MyTest()
			      {
			          {{arrange}}
			          
			          {{(isAsync ? "await " : "")}}[|{{xunitAssertion}}|];
			      }
			  }
			  """,
			$$"""
			  using System;
			  using System.Collections.Generic;
			  using System.Threading.Tasks;
			  using aweXpect;
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
