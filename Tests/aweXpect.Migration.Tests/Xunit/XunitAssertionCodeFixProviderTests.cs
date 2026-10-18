using Verifier =
	aweXpect.Migration.Tests.Verifiers.CSharpCodeFixVerifier<aweXpect.Migration.Analyzers.XunitAssertionAnalyzer,
		aweXpect.Migration.Analyzers.XunitAssertionCodeFixProvider>;

namespace aweXpect.Migration.Tests.Xunit;

public class XunitAssertionCodeFixProviderTests
{
	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Basic))]
	public async Task ShouldApplyCodeFixForBasicTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Boolean))]
	public async Task ShouldApplyCodeFixForBooleanTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Collections))]
	public async Task ShouldApplyCodeFixForCollectionTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Equality))]
	public async Task ShouldApplyCodeFixForEqualityTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Exceptions))]
	public async Task ShouldApplyCodeFixForExceptionTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Strings))]
	public async Task ShouldApplyCodeFixForStringTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Test]
	[MethodDataSource(typeof(TestCases), nameof(TestCases.Types))]
	public async Task ShouldApplyCodeFixForTypeTestCases(
		string xunitAssertion,
		string aweXpect,
		string arrange,
		bool isAsync) => await VerifyTestCase(xunitAssertion, aweXpect, arrange, isAsync);

	[Test]
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

	[Test]
	[Arguments("Assert.Equal(\"a\", \"A\", ignoreCase: true)")]
	[Arguments("Assert.Equal(\"a\", \"A\", StringComparer.OrdinalIgnoreCase)")]
	[Arguments("Assert.Contains(\"a\", \"A\", StringComparison.OrdinalIgnoreCase)")]
	[Arguments("Assert.Throws<ArgumentException>(testCode: () => {}, paramName: \"foo\")")]
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

	[Test]
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

	[Test]
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
