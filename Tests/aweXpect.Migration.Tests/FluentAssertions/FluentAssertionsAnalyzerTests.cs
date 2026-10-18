using aweXpect.Migration.Analyzers;
using Verifier =
	aweXpect.Migration.Tests.Verifiers.CSharpAnalyzerVerifier<aweXpect.Migration.Analyzers.FluentAssertionsAnalyzer>;

namespace aweXpect.Migration.Tests.FluentAssertions;

public class FluentAssertionsAnalyzerTests
{
	[Test]
	public async Task ShouldSupportActions() => await Verifier
		.VerifyAnalyzerAsync(
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
			        Action action = {|#0:() => true.Should().BeTrue()|};
			        
			        action();
			    }
			}
			""",
			Verifier.Diagnostic(Rules.FluentAssertionsRule)
				.WithLocation(0)
		);

	[Test]
	public async Task WhenUsingAssertEqual_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
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
			        
			        {|#0:subject.Should().Be(expected)|};
			    }
			}
			""",
			Verifier.Diagnostic(Rules.FluentAssertionsRule)
				.WithLocation(0)
		);

	[Test]
	public async Task WhenUsingAssertTrue_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        bool subject = true;
			        
			        {|#0:subject.Should().BeTrue()|};
			    }
			}
			""",
			Verifier.Diagnostic(Rules.FluentAssertionsRule)
				.WithLocation(0)
		);

	[Test]
	public async Task WhenUsingNestedShould_ShouldOnlyBeFlaggedOnce() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using aweXpect;
			using FluentAssertions;
			using Xunit;

			public class MyClass
			{
			    [Fact]
			    public void MyTest()
			    {
			        int[] subject = [];
			        
			        {|#0:subject.Should().AllSatisfy(item => item.Should().BeGreaterThan(0))|};
			    }
			}
			""",
			Verifier.Diagnostic(Rules.FluentAssertionsRule)
				.WithLocation(0)
		);

	[Test]
	public async Task WhenUsingNullableProperties_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
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
			        Exception subject = new Exception("foo");
			        
			        {|#0:subject?.Message.Should().Be(expected)|};
			    }
			}
			""",
			Verifier.Diagnostic(Rules.FluentAssertionsRule)
				.WithLocation(0)
		);
}
