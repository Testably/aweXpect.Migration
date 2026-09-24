using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace aweXpect.Migration.Analyzers;

/// <summary>
///     A code fix provider that migrates most xunit assertions to aweXpect.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XunitAssertionCodeFixProvider))]
[Shared]
public class XunitAssertionCodeFixProvider() : AssertionCodeFixProvider(Rules.XunitAssertionRule)
{
	/// <inheritdoc />
	protected override async Task<Document> ConvertAssertionAsync(CodeFixContext context,
		ExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
	{
		Document? document = context.Document;

		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

		if (root is not CompilationUnitSyntax compilationUnit ||
		    expressionSyntax is not InvocationExpressionSyntax invocationExpression)
		{
			return document;
		}

		if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
		{
			return document;
		}

		ArgumentSyntax? expected = invocationExpression.ArgumentList.Arguments.ElementAtOrDefault(0);
		ArgumentSyntax? actual = invocationExpression.ArgumentList.Arguments.ElementAtOrDefault(1) ??
		                         invocationExpression.ArgumentList.Arguments.ElementAtOrDefault(0);

		string? methodName = memberAccessExpressionSyntax.Name.Identifier.ValueText;

		string? genericArgs = GetGenericArguments(memberAccessExpressionSyntax.Name);

		ExpressionSyntax? newExpression = await GetNewExpression(context, memberAccessExpressionSyntax, methodName,
			actual, expected, genericArgs, invocationExpression.ArgumentList.Arguments);

		if (newExpression != null)
		{
			compilationUnit =
				compilationUnit.ReplaceNode(expressionSyntax, newExpression.WithTriviaFrom(expressionSyntax));
		}

		return document.WithSyntaxRoot(compilationUnit);
	}

#pragma warning disable S3776
	private static async Task<ExpressionSyntax?> GetNewExpression(CodeFixContext context,
		MemberAccessExpressionSyntax memberAccessExpressionSyntax, string method,
		ArgumentSyntax? actual, ArgumentSyntax? expected, string genericArgs,
		SeparatedSyntaxList<ArgumentSyntax> argumentListArguments)
	{
		bool isGeneric = !string.IsNullOrEmpty(genericArgs);

		return method switch
		{
			"Equal" => await IsEqualTo(context, memberAccessExpressionSyntax, argumentListArguments, actual, expected),
			"NotEqual" => await IsNotEqualTo(context, memberAccessExpressionSyntax, argumentListArguments, actual,
				expected),
			"Contains" => await Contains(context, memberAccessExpressionSyntax, actual, expected),
			"DoesNotContain" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).DoesNotContain({expected})"),
			"StartsWith" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).StartsWith({expected})"),
			"EndsWith" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).EndsWith({expected})"),
			"NotNull" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsNotNull()"),
			"Null" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsNull()"),
			"True" => SyntaxFactory.ParseExpression(
				actual == expected
					? $"Expect.That({actual}).IsTrue()"
					: $"Expect.That({expected}).IsTrue().Because({actual})"),
			"False" => SyntaxFactory.ParseExpression(
				actual == expected
					? $"Expect.That({actual}).IsFalse()"
					: $"Expect.That({expected}).IsFalse().Because({actual})"),
			"Same" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsSameAs({expected})"),
			"Distinct" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).All().AreUnique()"),
			"NotSame" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsNotSameAs({expected})"),
			"IsAssignableFrom" => isGeneric
				? SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).Is<{genericArgs}>()")
				: SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).Is({expected})"),
			"IsNotAssignableFrom" => isGeneric
				? SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsNot<{genericArgs}>()")
				: SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsNot({expected})"),
			"IsType" => isGeneric
				? SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsExactly<{genericArgs}>()")
				: SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsExactly({expected})"),
			"IsNotType" => isGeneric
				? SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsNotExactly<{genericArgs}>()")
				: SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsNotExactly({expected})"),
			"Empty" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsEmpty()"),
			"NotEmpty" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsNotEmpty()"),
			"Fail" => SyntaxFactory.ParseExpression(
				$"Fail.Test({expected})"),
			"Skip" => SyntaxFactory.ParseExpression(
				$"Skip.Test({expected})"),
			"Throws" or "ThrowsAsync" => isGeneric
				? SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).ThrowsExactly<{genericArgs}>()")
				: SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).ThrowsExactly({expected})"),
			"ThrowsAny" or "ThrowsAnyAsync" => isGeneric
				? SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).Throws<{genericArgs}>()")
				: SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).Throws({expected})"),
			_ => null,
		};
	}
#pragma warning restore S3776

	private static async Task<ExpressionSyntax> IsNotEqualTo(CodeFixContext context,
		MemberAccessExpressionSyntax memberAccessExpressionSyntax,
		SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
		ArgumentSyntax? actual,
		ArgumentSyntax? expected)
	{
		if (argumentListArguments.Count >= 3)
		{
			ExpressionSyntax? thirdArgument = argumentListArguments[2].Expression;
			SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();

			ISymbol? symbol = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol;

			if (symbol is IMethodSymbol { Parameters.Length: >= 3, } methodSymbol &&
			    (methodSymbol.Parameters[2].Type.Name.Equals("Double", StringComparison.Ordinal) ||
			     methodSymbol.Parameters[2].Type.Name.Equals("Float", StringComparison.Ordinal)))
			{
				return SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsNotEqualTo({expected}).Within({thirdArgument})");
			}
		}

		return SyntaxFactory.ParseExpression($"Expect.That({actual}).IsNotEqualTo({expected})");
	}

	private static async Task<ExpressionSyntax> IsEqualTo(CodeFixContext context,
		MemberAccessExpressionSyntax memberAccessExpressionSyntax,
		SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
		ArgumentSyntax? actual,
		ArgumentSyntax? expected)
	{
		if (argumentListArguments.Count >= 3)
		{
			ExpressionSyntax? thirdArgument = argumentListArguments[2].Expression;
			SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();

			ISymbol? symbol = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol;

			if (symbol is IMethodSymbol { Parameters.Length: >= 3, } methodSymbol &&
			    (methodSymbol.Parameters[2].Type.Name.Equals("Double", StringComparison.Ordinal) ||
			     methodSymbol.Parameters[2].Type.Name.Equals("Float", StringComparison.Ordinal) ||
			     methodSymbol.Parameters[2].Type.Name.Equals("TimeSpan", StringComparison.Ordinal)))
			{
				return SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).IsEqualTo({expected}).Within({thirdArgument})");
			}
		}

		return SyntaxFactory.ParseExpression($"Expect.That({actual}).IsEqualTo({expected})");
	}


	private static async Task<ExpressionSyntax> Contains(
		CodeFixContext context,
		MemberAccessExpressionSyntax memberAccessExpressionSyntax,
		ArgumentSyntax? actual,
		ArgumentSyntax? expected)
	{
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();

		ISymbol? symbol = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol;

		if (symbol is IMethodSymbol { Parameters.Length: 2, } methodSymbol &&
		    methodSymbol.Parameters[0].Type.Name == "IEnumerable" &&
		    methodSymbol.Parameters[1].Type.Name == "Predicate")
		{
			// Swap them - This overload is the other way around to the other ones.
			(actual, expected) = (expected, actual);
		}

		return SyntaxFactory.ParseExpression($"Expect.That({actual}).Contains({expected})");
	}

	private static string GetGenericArguments(ExpressionSyntax expressionSyntax)
	{
		if (expressionSyntax is GenericNameSyntax genericName)
		{
			return string.Join(", ", genericName.TypeArgumentList.Arguments.ToList());
		}

		return string.Empty;
	}
}
