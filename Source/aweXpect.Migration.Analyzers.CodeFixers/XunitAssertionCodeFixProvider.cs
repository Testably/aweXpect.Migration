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

		if (newExpression == null)
		{
			return document;
		}

		compilationUnit =
			compilationUnit.ReplaceNode(expressionSyntax, newExpression.WithTriviaFrom(expressionSyntax));
		if (newExpression.ToString().Contains("Math.Round("))
		{
			compilationUnit = await AddUsingIfMissing(document, compilationUnit, expressionSyntax.SpanStart,
				"System", "Math");
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
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
		IMethodSymbol? methodSymbol =
			semanticModel?.GetSymbolInfo(memberAccessExpressionSyntax).Symbol as IMethodSymbol;

		return method switch
		{
			"Equal" => Equality(methodSymbol, argumentListArguments, actual, expected, false),
			"NotEqual" => Equality(methodSymbol, argumentListArguments, actual, expected, true),
			"Contains" => Contains(methodSymbol, argumentListArguments, actual, expected, false),
			"DoesNotContain" => Contains(methodSymbol, argumentListArguments, actual, expected, true),
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
			"IsType" => TypeCheck(argumentListArguments, genericArgs, false),
			"IsNotType" => TypeCheck(argumentListArguments, genericArgs, true),
			"Empty" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsEmpty()"),
			"NotEmpty" => SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).IsNotEmpty()"),
			"Fail" => SyntaxFactory.ParseExpression(
				$"Fail.Test({expected})"),
			"Skip" => SyntaxFactory.ParseExpression(
				$"Skip.Test({expected})"),
			"Throws" or "ThrowsAsync" => Throws(methodSymbol, argumentListArguments, genericArgs, true),
			"ThrowsAny" or "ThrowsAnyAsync" => Throws(methodSymbol, argumentListArguments, genericArgs, false),
			_ => null,
		};
	}
#pragma warning restore S3776

	private static ExpressionSyntax? Equality(IMethodSymbol? methodSymbol,
		SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
		ArgumentSyntax? actual,
		ArgumentSyntax? expected,
		bool negated)
	{
		string expectation = negated ? "IsNotEqualTo" : "IsEqualTo";
		if (argumentListArguments.Count >= 3)
		{
			// Any other additional argument, e.g. a comparer or ignoreCase, would be lost in the rewrite.
			if (methodSymbol is not { Parameters.Length: >= 3, })
			{
				return null;
			}

			ExpressionSyntax thirdArgument = argumentListArguments[2].Expression;
			ITypeSymbol thirdParameterType = methodSymbol.Parameters[2].Type;
			if (thirdParameterType.SpecialType is SpecialType.System_Double or SpecialType.System_Single ||
			    thirdParameterType.Name == "TimeSpan")
			{
				return SyntaxFactory.ParseExpression(
					$"Expect.That({actual}).{expectation}({expected}).Within({thirdArgument})");
			}

			// The precision rounds both values to decimal places, which no tolerance can express.
			if (thirdParameterType.SpecialType == SpecialType.System_Int32 &&
			    methodSymbol.Parameters[0].Type.SpecialType is SpecialType.System_Double
				    or SpecialType.System_Single or SpecialType.System_Decimal)
			{
				string roundingArguments = $", {thirdArgument}" +
				                           (argumentListArguments.Count >= 4
					                           ? $", {argumentListArguments[3].Expression}"
					                           : "");
				return SyntaxFactory.ParseExpression(
					$"Expect.That(Math.Round({actual?.Expression}{roundingArguments}))" +
					$".{expectation}(Math.Round({expected?.Expression}{roundingArguments}))");
			}

			return null;
		}

		return SyntaxFactory.ParseExpression($"Expect.That({actual}).{expectation}({expected})");
	}

	private static ExpressionSyntax? Contains(
		IMethodSymbol? methodSymbol,
		SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
		ArgumentSyntax? actual,
		ArgumentSyntax? expected,
		bool negated)
	{
		if (argumentListArguments.Count > 2)
		{
			return null;
		}

		if (IsDictionaryOverload(methodSymbol))
		{
			return SyntaxFactory.ParseExpression(
				$"Expect.That({actual}).{(negated ? "DoesNotContainKey" : "ContainsKey")}({expected})");
		}

		if (methodSymbol is { Parameters.Length: 2, } &&
		    methodSymbol.Parameters[0].Type.Name is "IEnumerable" or "IAsyncEnumerable" &&
		    methodSymbol.Parameters[1].Type.Name == "Predicate")
		{
			// Swap them - This overload is the other way around to the other ones.
			(actual, expected) = (expected, actual);
		}

		return SyntaxFactory.ParseExpression(
			$"Expect.That({actual}).{(negated ? "DoesNotContain" : "Contains")}({expected})");
	}

	/// <summary>
	///     The overloads for dictionaries look up a key, whereas <c>Contains</c> in aweXpect looks for an entry.
	/// </summary>
	private static bool IsDictionaryOverload(IMethodSymbol? methodSymbol)
		=> methodSymbol?.OriginalDefinition is { TypeParameters.Length: 2, Parameters.Length: 2, };

	private static ExpressionSyntax? TypeCheck(
		SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
		string genericArgs,
		bool negated)
	{
		bool isGeneric = !string.IsNullOrEmpty(genericArgs);
		ArgumentSyntax? subject = argumentListArguments.ElementAtOrDefault(isGeneric ? 0 : 1);
		ArgumentSyntax? exactMatch = argumentListArguments.ElementAtOrDefault(isGeneric ? 1 : 2);
		bool isExactMatch = true;
		if (exactMatch is not null)
		{
			if (!exactMatch.Expression.IsKind(SyntaxKind.TrueLiteralExpression) &&
			    !exactMatch.Expression.IsKind(SyntaxKind.FalseLiteralExpression))
			{
				return null;
			}

			isExactMatch = exactMatch.Expression.IsKind(SyntaxKind.TrueLiteralExpression);
		}

		string expectation = (negated, isExactMatch) switch
		{
			(false, true) => "IsExactly",
			(false, false) => "Is",
			(true, true) => "IsNotExactly",
			(true, false) => "IsNot",
		};
		return SyntaxFactory.ParseExpression(isGeneric
			? $"Expect.That({subject?.Expression}).{expectation}<{genericArgs}>()"
			: $"Expect.That({subject?.Expression}).{expectation}({argumentListArguments.ElementAtOrDefault(0)?.Expression})");
	}

	private static ExpressionSyntax? Throws(
		IMethodSymbol? methodSymbol,
		SeparatedSyntaxList<ArgumentSyntax> argumentListArguments,
		string genericArgs,
		bool exactly)
	{
		string expectation = exactly ? "ThrowsExactly" : "Throws";
		if (argumentListArguments.Any(argument => argument.NameColon is not null))
		{
			return null;
		}

		if (string.IsNullOrEmpty(genericArgs))
		{
			return SyntaxFactory.ParseExpression(
				$"Expect.That({argumentListArguments.ElementAtOrDefault(1)}).{expectation}({argumentListArguments.ElementAtOrDefault(0)})");
		}

		if (methodSymbol is { Parameters.Length: > 1, } &&
		    methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String)
		{
			return SyntaxFactory.ParseExpression(
				$"Expect.That({argumentListArguments.ElementAtOrDefault(1)?.Expression}).{expectation}<{genericArgs}>()" +
				$".WithParamName({argumentListArguments.ElementAtOrDefault(0)?.Expression})");
		}

		return SyntaxFactory.ParseExpression(
			$"Expect.That({argumentListArguments.ElementAtOrDefault(0)?.Expression}).{expectation}<{genericArgs}>()");
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
