using aweXpect.Migration.Analyzers;
using Verifier =
	aweXpect.Migration.Tests.Verifiers.CSharpAnalyzerVerifier<aweXpect.Migration.Analyzers.XunitAssertionAnalyzer>;

namespace aweXpect.Migration.Tests.Xunit;

public class XunitAssertionAnalyzerTests
{
	[Test]
	public async Task WhenUsingAssertEqual_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
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
			        
			        {|#0:Assert.Equal(expected, subject)|};
			    }
			}
			""",
			Verifier.Diagnostic(Rules.XunitAssertionRule)
				.WithLocation(0)
		);

	[Test]
	public async Task WhenUsingAssertTrue_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using aweXpect;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        bool subject = true;
			        
			        {|#0:Assert.True(subject)|};
			    }
			}
			""",
			Verifier.Diagnostic(Rules.XunitAssertionRule)
				.WithLocation(0)
		);
}
