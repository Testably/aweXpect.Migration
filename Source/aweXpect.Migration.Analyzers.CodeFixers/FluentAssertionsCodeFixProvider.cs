using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using aweXpect.Migration.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable S1192 // String literals should not be duplicated
namespace aweXpect.Migration.Analyzers;

/// <summary>
///     A code fix provider that migrates most assertions from FluentAssertions to aweXpect.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FluentAssertionsCodeFixProvider))]
[Shared]
public class FluentAssertionsCodeFixProvider() : AssertionCodeFixProvider(Rules.FluentAssertionsRule)
{
	/// <inheritdoc />
	protected override async Task<Document> ConvertAssertionAsync(CodeFixContext context,
		ExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
	{
		Document? document = context.Document;

		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

		if (root is not CompilationUnitSyntax compilationUnit)
		{
			return document;
		}

		ExpressionSyntaxWalker walker = new(expressionSyntax is ConditionalAccessExpressionSyntax);
		walker.Visit(expressionSyntax);
		ExpressionSyntax? actual = walker.Subject;
		if (actual is null || walker.Methods.Count == 0)
		{
			return document;
		}

		SyntaxNode nodeToReplace = expressionSyntax;
		if (expressionSyntax is LambdaExpressionSyntax lambdaExpressionSyntax)
		{
			nodeToReplace = lambdaExpressionSyntax.Body;
		}

		ExpressionSyntax? newExpression = await GetNewExpression(context,
			actual, walker.Methods, expressionSyntax is LambdaExpressionSyntax);

		if (newExpression != null)
		{
			compilationUnit =
				compilationUnit.ReplaceNode(nodeToReplace, newExpression.WithTriviaFrom(expressionSyntax));
			return document.WithSyntaxRoot(compilationUnit);
		}

		return document;
	}

	private static bool IsString(ISymbol symbol)
		=> symbol.Name.Equals(nameof(String), StringComparison.OrdinalIgnoreCase);

#pragma warning disable S3776
	private static async Task<string?> BeEquivalentTo(CodeFixContext context,
		SeparatedSyntaxList<ArgumentSyntax> arguments,
		ExpressionSyntax actual, ArgumentSyntax? expected, Stack<IDefinitionElement>? methods,
		bool negated = false)
	{
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
		ISymbol? actualSymbol = semanticModel.GetSymbolInfo(actual).Symbol;
		if (semanticModel is not null && actualSymbol is not null &&
		    GetType(actualSymbol) is { } actualSymbolType && IsEnumerable(actualSymbolType))
		{
			string expressionSuffix = IsString(actualSymbolType) ? ".IgnoringCase()" : ".InAnyOrder()";
			int becauseIndex = 1;
			string? secondArgument = arguments.ElementAtOrDefault(1)?.ToString() ?? "";
			if (!secondArgument.StartsWith("\"") || !secondArgument.EndsWith("\""))
			{
				if (secondArgument.Contains(".WithStrictOrdering()"))
				{
					expressionSuffix = "";
				}

				if (secondArgument.Contains(".IgnoringCase()") && !expressionSuffix.Contains(".IgnoringCase()"))
				{
					expressionSuffix += ".IgnoringCase()";
				}

				if (secondArgument.Contains(".IgnoringLeadingWhitespace()"))
				{
					expressionSuffix += ".IgnoringLeadingWhiteSpace()";
				}

				if (secondArgument.Contains(".IgnoringTrailingWhitespace()"))
				{
					expressionSuffix += ".IgnoringTrailingWhiteSpace()";
				}

				if (secondArgument.Contains(".IgnoringNewlineStyle()"))
				{
					expressionSuffix += ".IgnoringNewlineStyle()";
				}

				becauseIndex++;
			}

			return await ParseExpressionWithBecauseSupport(context, actual, arguments,
				$".{(negated ? "IsNotEqualTo" : "IsEqualTo")}({expected})" + expressionSuffix,
				methods, becauseIndex);
		}

		return await ParseExpressionWithBecauseSupport(context, actual, arguments,
			$".{(negated ? "IsNotEquivalentTo" : "IsEquivalentTo")}({expected})",
			methods, 1);
	}
#pragma warning restore S3776

	private static async Task<string?> BeInRange(
		CodeFixContext context,
		SeparatedSyntaxList<ArgumentSyntax> arguments,
		ExpressionSyntax actual,
		Stack<IDefinitionElement>? methods,
		bool negated = false)
	{
		if (arguments.Count >= 2)
		{
			return await ParseExpressionWithBecauseSupport(context, actual, arguments,
				$".{(negated ? "IsNotBetween" : "IsBetween")}({arguments[0]}).And({arguments[1]})",
				methods, 2);
		}

		return null;
	}

#pragma warning disable S3776
	private static async Task<string?> BeInOrder(
		SortOrder order,
		CodeFixContext context,
		MethodDefinition mainMethod,
		SeparatedSyntaxList<ArgumentSyntax> arguments,
		ExpressionSyntax actual,
		Stack<IDefinitionElement>? methods,
		bool negated = false)
	{
		if (mainMethod.Arguments.Count > 0)
		{
			SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
			ISymbol? symbol = semanticModel.GetSymbolInfo(mainMethod.Method).Symbol;

			if (symbol is IMethodSymbol { Parameters.Length: > 0 } methodSymbol &&
			    !IsString(methodSymbol.Parameters[0].Type))
			{
				if (methodSymbol.Parameters[0].Type.Name.StartsWith("IComparer"))
				{
					return await ParseExpressionWithBecauseSupport(context, actual, arguments,
						$".{(negated ? "IsNotIn" : "IsIn")}{order}Order().Using({arguments[0]})",
						methods, 1);
				}

				if (methodSymbol.Parameters.Length > 1 &&
				    methodSymbol.Parameters[1].Type.Name.StartsWith("IComparer"))
				{
					return await ParseExpressionWithBecauseSupport(context, actual, arguments,
						$".{(negated ? "IsNotIn" : "IsIn")}{order}Order({arguments[0]}).Using({arguments[1]})",
						methods, 2);
				}

				return await ParseExpressionWithBecauseSupport(context, actual, arguments,
					$".{(negated ? "IsNotIn" : "IsIn")}{order}Order({arguments[0]})",
					methods, 1);
			}
		}

		return await ParseExpressionWithBecauseSupport(context, actual, arguments,
			$".{(negated ? "IsNotIn" : "IsIn")}{order}Order()",
			methods, 0);
	}
#pragma warning restore S3776

#pragma warning disable S3776
	private static async Task<string?> AllBeEquivalentTo(CodeFixContext context,
		SeparatedSyntaxList<ArgumentSyntax> arguments,
		ExpressionSyntax actual, ArgumentSyntax? expected, Stack<IDefinitionElement>? methods,
		bool negated = false)
	{
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
		ISymbol? actualSymbol = semanticModel.GetSymbolInfo(actual).Symbol;
		if (semanticModel is not null && actualSymbol is not null &&
		    GetType(actualSymbol) is { } actualSymbolType && IsStringEnumerable(actualSymbolType))
		{
			string expressionSuffix = "";
			int becauseIndex = 1;
			string? secondArgument = arguments.ElementAtOrDefault(1)?.ToString() ?? "";
			if (!secondArgument.StartsWith("\"") || !secondArgument.EndsWith("\""))
			{
				if (secondArgument.Contains(".WithoutStrictOrdering()"))
				{
					expressionSuffix += ".InAnyOrder()";
				}

				if (secondArgument.Contains(".IgnoringCase()"))
				{
					expressionSuffix += ".IgnoringCase()";
				}

				if (secondArgument.Contains(".IgnoringLeadingWhitespace()"))
				{
					expressionSuffix += ".IgnoringLeadingWhiteSpace()";
				}

				if (secondArgument.Contains(".IgnoringTrailingWhitespace()"))
				{
					expressionSuffix += ".IgnoringTrailingWhiteSpace()";
				}

				if (secondArgument.Contains(".IgnoringNewlineStyle()"))
				{
					expressionSuffix += ".IgnoringNewlineStyle()";
				}

				becauseIndex++;
			}

			return await ParseExpressionWithBecauseSupport(context, actual, arguments,
				$".All().{(negated ? "AreNotEqualTo" : "AreEqualTo")}({expected})" +
				expressionSuffix,
				methods, becauseIndex);
		}

		return await ParseExpressionWithBecauseSupport(context, actual, arguments,
			$".All().{(negated ? "AreNotEquivalentTo" : "AreEquivalentTo")}({expected})",
			methods, 1);
	}
#pragma warning restore S3776

	private static async Task<string?> BeOneOf(
		CodeFixContext context,
		MethodDefinition mainMethod,
		ExpressionSyntax actual,
		Stack<IDefinitionElement>? methods)
	{
		if (mainMethod.Arguments.Count > 1)
		{
			SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
			ISymbol? symbol = semanticModel.GetSymbolInfo(mainMethod.Method).Symbol;

			if (symbol is IMethodSymbol { Parameters.Length: > 1 } methodSymbol &&
			    methodSymbol.Parameters[0].Type.Name != methodSymbol.Parameters[1].Type.Name)
			{
				return await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
					$".IsOneOf({mainMethod.Arguments[0]})",
					methods, 1);
			}
		}

		string? arguments = string.Join(", ", mainMethod.Arguments.Select(x => x.ToString()));
		return await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
			$".IsOneOf({arguments})",
			methods);
	}

	private static async Task<string?> Contain(
		CodeFixContext context,
		MethodDefinition mainMethod,
		ExpressionSyntax actual,
		ArgumentSyntax? expected,
		Stack<IDefinitionElement>? methods)
	{
		int becauseIndex = 1;
		string expressionSuffix = "";
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
		ISymbol? symbol = semanticModel?.GetSymbolInfo(mainMethod.Method).Symbol;
		if (symbol is IMethodSymbol { Parameters.Length: > 0 } methodSymbol &&
		    !IsString(methodSymbol.Parameters[0].Type) && IsEnumerable(methodSymbol.Parameters[0].Type))
		{
			expressionSuffix = ".InAnyOrder().IgnoringInterspersedItems()";
		}
		else if (mainMethod.Arguments.Count > 1)
		{
			string? occurrenceConstraint = mainMethod.Arguments[1].ToString();
			expressionSuffix = occurrenceConstraint switch
			{
				"AtLeast.Once()" => ".AtLeast().Once()",
				"AtLeast.Twice()" => ".AtLeast().Twice()",
				"AtLeast.Thrice()" => ".AtLeast(3.Times())",
				"AtMost.Once()" => ".AtMost().Once()",
				"AtMost.Twice()" => ".AtMost().Twice()",
				"AtMost.Thrice()" => ".AtMost(3.Times())",
				"LessThan.Twice()" => ".LessThan().Twice()",
				"LessThan.Thrice()" => ".LessThan(3.Times())",
				"MoreThan.Once()" => ".MoreThan().Once()",
				"MoreThan.Twice()" => ".MoreThan().Twice()",
				"MoreThan.Thrice()" => ".MoreThan(3.Times())",
				"Exactly.Once()" => ".Once()",
				"Exactly.Twice()" => ".Twice()",
				"Exactly.Thrice()" => ".Exactly(3.Times())",
				_ => ""
			};
			if (TryExtract(occurrenceConstraint, "AtLeast.Times(", out string? atLeastTimes))
			{
				expressionSuffix = $".AtLeast({atLeastTimes})";
			}
			else if (TryExtract(occurrenceConstraint, "AtMost.Times(", out string? atMostTimes))
			{
				expressionSuffix = $".AtMost({atMostTimes})";
			}
			else if (TryExtract(occurrenceConstraint, "Exactly.Times(", out string? exactlyTimes))
			{
				expressionSuffix = $".Exactly({exactlyTimes})";
			}
			else if (TryExtract(occurrenceConstraint, "LessThan.Times(", out string? lessThanTimes))
			{
				expressionSuffix = $".LessThan({lessThanTimes})";
			}
			else if (TryExtract(occurrenceConstraint, "MoreThan.Times(", out string? moreThanTimes))
			{
				expressionSuffix = $".MoreThan({moreThanTimes})";
			}

			static bool TryExtract(string input, string prefix, out string? times)
			{
				if (input.StartsWith(prefix) && input.EndsWith(")"))
				{
					times = input.Substring(prefix.Length, input.Length - prefix.Length - 1);
					return true;
				}

				times = null;
				return false;
			}

			if (expressionSuffix != "")
			{
				becauseIndex++;
			}
		}

		return await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
			$".Contains({expected}){expressionSuffix}",
			methods, becauseIndex);
	}

	private static async Task<string?> ContainInOrder(
		CodeFixContext context,
		MethodDefinition mainMethod,
		ExpressionSyntax actual,
		Stack<IDefinitionElement>? methods,
		bool isConsecutive)
	{
		string expressionSuffix = isConsecutive ? "" : ".IgnoringInterspersedItems()";
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
		ISymbol? symbol = semanticModel?.GetSymbolInfo(mainMethod.Method).Symbol;
		if (symbol is IMethodSymbol { Parameters.Length: > 0 } methodSymbol &&
		    ((mainMethod.Arguments.Count == 1 && methodSymbol.Parameters.Length == 1 &&
		      !IsString(methodSymbol.Parameters[0].Type) && IsEnumerable(methodSymbol.Parameters[0].Type)) ||
		     (methodSymbol.Parameters.Length > 1 && IsString(methodSymbol.Parameters[1].Type)))
		   )
		{
			return await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
				$".Contains({mainMethod.Arguments[0]}){expressionSuffix}",
				methods, 1);
		}

		return await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
			$".Contains([{string.Join(", ", mainMethod.Arguments)}]){expressionSuffix}",
			methods);
	}

	private static async Task<string?> Match(
		CodeFixContext context,
		MethodDefinition mainMethod,
		ExpressionSyntax actual,
		Stack<IDefinitionElement>? methods)
	{
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
		ISymbol? symbol = semanticModel?.GetSymbolInfo(mainMethod.Method).Symbol;
		if (symbol is IMethodSymbol { Parameters.Length: > 0 } methodSymbol &&
		    IsString(methodSymbol.Parameters[0].Type))
		{
			return await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
				$".IsEqualTo({mainMethod.Arguments[0]}).AsWildcard()",
				methods, 1);
		}

		return await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
			$".Satisfies({mainMethod.Arguments[0]})",
			methods, 1);
	}

	private static async Task<string?> ContainEquivalentOf(
		CodeFixContext context,
		MethodDefinition mainMethod,
		ExpressionSyntax actual,
		Stack<IDefinitionElement>? methods,
		bool isNegated)
		=> await ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments,
			$".{(isNegated ? "DoesNotContain" : "Contains")}({mainMethod.Arguments[0]}).Equivalent()",
			methods, 1);

	private static async Task<string?> ParseExpressionWithBecauseSupport(
		CodeFixContext context,
		ExpressionSyntax actual,
		SeparatedSyntaxList<ArgumentSyntax> arguments,
		string expression,
		Stack<IDefinitionElement>? methods,
		int? becauseIndex = null)
	{
		if (methods?.Count > 0)
		{
			foreach (IDefinitionElement? method in methods)
			{
				expression += await ParseAdditionalMethodExpression(context, actual, method) ?? "";
			}
		}

		if (becauseIndex.HasValue)
		{
			string? because = arguments.ElementAtOrDefault(becauseIndex.Value)?.ToString();
			if (because != null)
			{
				if (becauseIndex.Value + 1 < arguments.Count)
				{
					because = $"${because}";
					int index = 0;
					for (int i = becauseIndex.Value + 1; i < arguments.Count; i++)
					{
						because = because.Replace($"{{{index++}}}", $"{{{arguments[i]}}}");
					}
				}

				expression += $".Because({because})";
			}
		}

		return expression;
	}

	private static async Task<string?> ParseAdditionalMethodExpression(
		CodeFixContext context,
		ExpressionSyntax actual,
		IDefinitionElement definitionElement)
	{
		if (definitionElement is MethodDefinitionElement methodDefinitionElement)
		{
			MethodDefinition? method = methodDefinitionElement.Element;
			string? methodName = method.Method.Name.Identifier.ValueText;
			return methodName switch
			{
				"WithMessage" => $".WithMessage({method.Arguments.ElementAtOrDefault(0)}).AsWildcard()",
				_ => await GetNewExpressionFor(context, actual, method, null)
			};
		}

		if (definitionElement is AndDefinitionElement)
		{
			return ".And";
		}

		return null;
	}

	private static ITypeSymbol? GetType(ISymbol symbol)
	{
		if (symbol is ITypeSymbol typeSymbol)
		{
			return typeSymbol;
		}

		if (symbol is IPropertySymbol propertySymbol)
		{
			return propertySymbol.Type;
		}

		if (symbol is IFieldSymbol fieldSymbol)
		{
			return fieldSymbol.Type;
		}

		if (symbol is ILocalSymbol localSymbol)
		{
			return localSymbol.Type;
		}

		return null;
	}

	private static bool IsEnumerable(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is IArrayTypeSymbol)
		{
			return true;
		}

		if (typeSymbol is INamedTypeSymbol namedTypeSymbol
		    && namedTypeSymbol.GloballyQualifiedNonGeneric() is "global::System.Collections.IEnumerable"
			    or "global::System.Collections.Generic.IEnumerable")
		{
			return true;
		}

		return typeSymbol.AllInterfaces.Any(i => i.GloballyQualified() == "global::System.Collections.IEnumerable");
	}

	private static bool IsStringEnumerable(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is IArrayTypeSymbol arraySymbol)
		{
			return arraySymbol.ElementType.Name.Equals(nameof(String), StringComparison.OrdinalIgnoreCase);
		}

		if (typeSymbol is INamedTypeSymbol namedTypeSymbol
		    && namedTypeSymbol.GloballyQualifiedNonGeneric() is "global::System.Collections.IEnumerable"
			    or "global::System.Collections.Generic.IEnumerable")
		{
			return namedTypeSymbol.TypeArguments[0].Name.Equals(nameof(String), StringComparison.OrdinalIgnoreCase);
		}

		return typeSymbol.AllInterfaces.Any(i => i.GloballyQualified() == "global::System.Collections.IEnumerable");
	}

	private static string GetGenericArguments(ExpressionSyntax expressionSyntax)
	{
		if (expressionSyntax is GenericNameSyntax genericName)
		{
			return string.Join(", ", genericName.TypeArgumentList.Arguments.ToList());
		}

		return string.Empty;
	}

	private static async Task<ExpressionSyntax?> GetNewExpression(
		CodeFixContext context,
		ExpressionSyntax actual,
		Stack<IDefinitionElement> methods,
		bool wrapSynchronously)
	{
		IDefinitionElement? mainMethodDefinition = methods.Pop();
		if (mainMethodDefinition is not MethodDefinitionElement methodDefinitionElement)
		{
			return null;
		}

		MethodDefinition mainMethod = methodDefinitionElement.Element;

		string? newExpression = await GetNewExpressionFor(context, actual, mainMethod, methods);
		if (newExpression != null)
		{
			newExpression = $"Expect.That({actual}){newExpression}";
			if (wrapSynchronously)
			{
				newExpression = $"aweXpect.Synchronous.Synchronously.Verify({newExpression})";
			}

			return SyntaxFactory.ParseExpression(newExpression);
		}

		return null;
	}

#pragma warning disable S3776
	private static async Task<string?> GetNewExpressionFor(
		CodeFixContext context,
		ExpressionSyntax actual,
		MethodDefinition mainMethod,
		Stack<IDefinitionElement>? methods)
	{
		Task<string?> ParseExpressionWithBecause(string expression, int? becauseIndex = null)
			=> ParseExpressionWithBecauseSupport(context, actual, mainMethod.Arguments, expression, methods,
				becauseIndex);

		MemberAccessExpressionSyntax? memberAccessExpressionSyntax = mainMethod.Method;
		ArgumentSyntax? expected = mainMethod.Arguments.ElementAtOrDefault(0);
		string? expectedType = null;

		if (expected is not null)
		{
			SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync();
			ISymbol? symbol = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol;

			if (symbol is IMethodSymbol { Parameters.Length: > 0 } methodSymbol)
			{
				expectedType = methodSymbol.Parameters[0].Type.Name;
			}
		}

		string? methodName = memberAccessExpressionSyntax.Name.Identifier.ValueText;
		string? genericArgs = GetGenericArguments(memberAccessExpressionSyntax.Name);
		bool isGeneric = !string.IsNullOrEmpty(genericArgs);
		return methodName switch
		{
			"Be" => await ParseExpressionWithBecause(
				$".IsEqualTo({expected})", 1),
			"NotBe" => await ParseExpressionWithBecause(
				$".IsNotEqualTo({expected})", 1),
			"BeEquivalentTo" => await BeEquivalentTo(context, mainMethod.Arguments, actual, expected,
				methods),
			"NotBeEquivalentTo" => await BeEquivalentTo(context, mainMethod.Arguments, actual, expected,
				methods, true),
			"Contain" => await Contain(context, mainMethod, actual, expected, methods),
			"NotContain" => await ParseExpressionWithBecause(
				$".DoesNotContain({expected})", 1),
			"StartWith" => await ParseExpressionWithBecause(
				$".StartsWith({expected})", 1),
			"NotStartWith" => await ParseExpressionWithBecause(
				$".DoesNotStartWith({expected})", 1),
			"EndWith" => await ParseExpressionWithBecause(
				$".EndsWith({expected})", 1),
			"NotEndWith" => await ParseExpressionWithBecause(
				$".DoesNotEndWith({expected})", 1),
			"BeSubsetOf" => await ParseExpressionWithBecause(
				$".IsContainedIn({expected}).InAnyOrder()", 1),
			"NotBeSubsetOf" => await ParseExpressionWithBecause(
				$".IsNotContainedIn({expected}).InAnyOrder()", 1),
			"Match" => await Match(context, mainMethod, actual, methods),
			"ContainInOrder" => await ContainInOrder(context, mainMethod, actual, methods, false),
			"ContainInConsecutiveOrder" => await ContainInOrder(context, mainMethod, actual, methods, true),
			"ContainEquivalentOf" => await ContainEquivalentOf(context, mainMethod, actual, methods, false),
			"NotContainEquivalentOf" => await ContainEquivalentOf(context, mainMethod, actual, methods, true),
			"BeInAscendingOrder" => await BeInOrder(
				SortOrder.Ascending, context, mainMethod, mainMethod.Arguments, actual, methods),
			"NotBeInAscendingOrder" => await BeInOrder(
				SortOrder.Ascending, context, mainMethod, mainMethod.Arguments, actual, methods, true),
			"BeInDescendingOrder" => await BeInOrder(
				SortOrder.Descending, context, mainMethod, mainMethod.Arguments, actual, methods),
			"NotBeInDescendingOrder" => await BeInOrder(
				SortOrder.Descending, context, mainMethod, mainMethod.Arguments, actual, methods, true),
			"OnlyHaveUniqueItems" => await ParseExpressionWithBecause(
				".AreAllUnique()", 0),
			"BeEmpty" => await ParseExpressionWithBecause(
				".IsEmpty()", 0),
			"NotBeEmpty" => await ParseExpressionWithBecause(
				".IsNotEmpty()", 0),
			"BeNullOrEmpty" => await ParseExpressionWithBecause(
				".IsNullOrEmpty()", 0),
			"NotBeNullOrEmpty" => await ParseExpressionWithBecause(
				".IsNotNullOrEmpty()", 0),
			"BeNullOrWhiteSpace" => await ParseExpressionWithBecause(
				".IsNullOrWhiteSpace()", 0),
			"NotBeNullOrWhiteSpace" => await ParseExpressionWithBecause(
				".IsNotNullOrWhiteSpace()", 0),
			"BeInRange" => await BeInRange(context, mainMethod.Arguments, actual,
				methods),
			"NotBeInRange" => await BeInRange(context, mainMethod.Arguments, actual,
				methods, true),
			"BePositive" => await ParseExpressionWithBecause(
				".IsPositive()", 0),
			"BeNegative" => await ParseExpressionWithBecause(
				".IsNegative()", 0),
			"BeGreaterThan" => await ParseExpressionWithBecause(
				$".IsGreaterThan({expected})", 1),
			"BeGreaterThanOrEqualTo" => await ParseExpressionWithBecause(
				$".IsGreaterThanOrEqualTo({expected})", 1),
			"BeGreaterOrEqualTo" => await ParseExpressionWithBecause(
				$".IsGreaterThanOrEqualTo({expected})", 1),
			"BeLessThan" => await ParseExpressionWithBecause(
				$".IsLessThan({expected})", 1),
			"BeLessOrEqualTo" => await ParseExpressionWithBecause(
				$".IsLessThanOrEqualTo({expected})", 1),
			"BeLessThanOrEqualTo" => await ParseExpressionWithBecause(
				$".IsLessThanOrEqualTo({expected})", 1),
			"BeApproximately" => await ParseExpressionWithBecause(
				$".IsEqualTo({expected}).Within({mainMethod.Arguments.ElementAtOrDefault(1)})",
				2),
			"NotBeApproximately" => await ParseExpressionWithBecause(
				$".IsNotEqualTo({expected}).Within({mainMethod.Arguments.ElementAtOrDefault(1)})",
				2),
			"BeCloseTo" => await ParseExpressionWithBecause(
				$".IsEqualTo({expected}).Within({mainMethod.Arguments.ElementAtOrDefault(1)})",
				2),
			"NotBeCloseTo" => await ParseExpressionWithBecause(
				$".IsNotEqualTo({expected}).Within({mainMethod.Arguments.ElementAtOrDefault(1)})",
				2),
			"BeAfter" => await ParseExpressionWithBecause(
				$".IsAfter({expected})", 1),
			"BeOnOrAfter" => await ParseExpressionWithBecause(
				$".IsOnOrAfter({expected})", 1),
			"BeBefore" => await ParseExpressionWithBecause(
				$".IsBefore({expected})", 1),
			"BeOnOrBefore" => await ParseExpressionWithBecause(
				$".IsOnOrBefore({expected})", 1),
			"NotBeAfter" => await ParseExpressionWithBecause(
				$".IsNotAfter({expected})", 1),
			"NotBeOnOrAfter" => await ParseExpressionWithBecause(
				$".IsNotOnOrAfter({expected})", 1),
			"NotBeBefore" => await ParseExpressionWithBecause(
				$".IsNotBefore({expected})", 1),
			"NotBeOnOrBefore" => await ParseExpressionWithBecause(
				$".IsNotOnOrBefore({expected})", 1),
			"NotBeNull" => await ParseExpressionWithBecause(
				".IsNotNull()", 0),
			"BeNull" => await ParseExpressionWithBecause(
				".IsNull()", 0),
			"BeTrue" => await ParseExpressionWithBecause(
				".IsTrue()", 0),
			"BeFalse" => await ParseExpressionWithBecause(
				".IsFalse()", 0),
			"NotBeTrue" => await ParseExpressionWithBecause(
				".IsNotTrue()", 0),
			"NotBeFalse" => await ParseExpressionWithBecause(
				".IsNotFalse()", 0),
			"Imply" => await ParseExpressionWithBecause(
				$".Implies({expected})", 1),
			"BeDefined" => await ParseExpressionWithBecause(
				".IsDefined()", 0),
			"NotBeDefined" => await ParseExpressionWithBecause(
				".IsNotDefined()", 0),
			"HaveYear" => await ParseExpressionWithBecause(
				$".HasYear().EqualTo({expected})", 1),
			"HaveMonth" => await ParseExpressionWithBecause(
				$".HasMonth().EqualTo({expected})", 1),
			"HaveDay" => await ParseExpressionWithBecause(
				$".HasDay().EqualTo({expected})", 1),
			"HaveHour" => await ParseExpressionWithBecause(
				$".HasHour().EqualTo({expected})", 1),
			"HaveMinute" => await ParseExpressionWithBecause(
				$".HasMinute().EqualTo({expected})", 1),
			"HaveSecond" => await ParseExpressionWithBecause(
				$".HasSecond().EqualTo({expected})", 1),
			"HaveOffset" => await ParseExpressionWithBecause(
				$".HasOffset().EqualTo({expected})", 1),
			"NotHaveYear" => await ParseExpressionWithBecause(
				$".HasYear().NotEqualTo({expected})", 1),
			"NotHaveMonth" => await ParseExpressionWithBecause(
				$".HasMonth().NotEqualTo({expected})", 1),
			"NotHaveDay" => await ParseExpressionWithBecause(
				$".HasDay().NotEqualTo({expected})", 1),
			"NotHaveHour" => await ParseExpressionWithBecause(
				$".HasHour().NotEqualTo({expected})", 1),
			"NotHaveMinute" => await ParseExpressionWithBecause(
				$".HasMinute().NotEqualTo({expected})", 1),
			"NotHaveSecond" => await ParseExpressionWithBecause(
				$".HasSecond().NotEqualTo({expected})", 1),
			"NotHaveOffset" => await ParseExpressionWithBecause(
				$".HasOffset().NotEqualTo({expected})", 1),
			"HaveValue" => expected is null || expected.Expression.ToString().Contains('\"')
				? await ParseExpressionWithBecause(
					".IsNotNull()", 0)
				: await ParseExpressionWithBecause(
					$".HasValue({expected})", 1),
			"NotHaveValue" => expected is null || expected.Expression.ToString().Contains('\"')
				? await ParseExpressionWithBecause(
					".IsNull()", 0)
				: await ParseExpressionWithBecause(
					$".DoesNotHaveValue({expected})", 1),
			"HaveFlag" => await ParseExpressionWithBecause(
				$".HasFlag({expected})", 1),
			"NotHaveFlag" => await ParseExpressionWithBecause(
				$".DoesNotHaveFlag({expected})", 1),
			"BeSameAs" => await ParseExpressionWithBecause(
				$".IsSameAs({expected})", 1),
			"NotBeSameAs" => await ParseExpressionWithBecause(
				$".IsNotSameAs({expected})", 1),
			"BeOneOf" => await BeOneOf(context, mainMethod, actual, methods),
			"HaveCount" => await ParseExpressionWithBecause(
				$".HasCount({expected})", 1),
			"OnlyContain" => await ParseExpressionWithBecause(
				$".All().Satisfy({expected})", 1),
			"ContainSingle" =>
				expectedType == null || expectedType.Equals(nameof(String), StringComparison.OrdinalIgnoreCase)
					? await ParseExpressionWithBecause(".HasSingle()", 0)
					: await ParseExpressionWithBecause($".HasSingle().Matching({expected})", 1),
			"AllBeAssignableTo" => isGeneric
				? await ParseExpressionWithBecause(
					$".All().Are<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".All().Are({expected})", 1),
			"AllBeEquivalentTo" => await AllBeEquivalentTo(context, mainMethod.Arguments, actual, expected,
				methods),
			"AllBeOfType" => isGeneric
				? await ParseExpressionWithBecause(
					$".All().AreExactly<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".All().AreExactly({expected})", 1),
			"BeAssignableTo" => isGeneric
				? await ParseExpressionWithBecause(
					$".Is<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".Is({expected})", 1),
			"NotBeAssignableTo" => isGeneric
				? await ParseExpressionWithBecause(
					$".IsNot<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".IsNot({expected})", 1),
			"BeOfType" => isGeneric
				? await ParseExpressionWithBecause(
					$".IsExactly<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".IsExactly({expected})", 1),
			"NotBeOfType" => isGeneric
				? await ParseExpressionWithBecause(
					$".IsNotExactly<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".IsNotExactly({expected})", 1),
			"NotThrow" or "NotThrowAsync" => isGeneric
				? await ParseExpressionWithBecause(
					$".DoesNotThrow<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					".DoesNotThrow()", 0),
			"Throw" or "ThrowAsync" => isGeneric
				? await ParseExpressionWithBecause(
					$".Throws<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".Throws({expected})", 1),
			"ThrowExactly" or "ThrowExactlyAsync" => isGeneric
				? await ParseExpressionWithBecause(
					$".ThrowsExactly<{genericArgs}>()", 0)
				: await ParseExpressionWithBecause(
					$".ThrowsExactly({expected})", 1),
			_ => null
		};
	}
#pragma warning restore S3776

	private enum SortOrder
	{
		Ascending,
		Descending
	}

	private sealed class MethodDefinition
	{
		public MethodDefinition(MemberAccessExpressionSyntax method)
		{
			Method = method;
			InvocationExpressionSyntax? invocationExpressionSyntax = method.Parent as InvocationExpressionSyntax;
			Arguments = invocationExpressionSyntax?.ArgumentList.Arguments ?? [];
		}

		public MemberAccessExpressionSyntax Method { get; }
		public SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }
	}

	private interface IDefinitionElement;

	private sealed class MethodDefinitionElement(MethodDefinition methodDefinition) : IDefinitionElement
	{
		public MethodDefinition Element => methodDefinition;
	}

	private sealed class AndDefinitionElement : IDefinitionElement;

	private sealed class ExpressionSyntaxWalker(bool isConditional) : SyntaxWalker
	{
		private bool _isConditional = isConditional;
		private bool _isShould;
		private string _subjectString = "";
		public ExpressionSyntax? Subject { get; private set; }

		public Stack<IDefinitionElement> Methods { get; } = [];

		public override void Visit(SyntaxNode node)
		{
			if (_isConditional)
			{
				if (_subjectString == "" && node is IdentifierNameSyntax identifierNameSyntax)
				{
					_subjectString = identifierNameSyntax.ToString();
				}

				if (node is MemberBindingExpressionSyntax memberBindingExpressionSyntax)
				{
					_subjectString += "?" + memberBindingExpressionSyntax;
				}

				if (node is ArgumentListSyntax invocationExpressionSyntax)
				{
					_subjectString += invocationExpressionSyntax;
				}
			}

			if (_isShould && node is not ParenthesizedExpressionSyntax)
			{
				Subject = node as ExpressionSyntax;
				_isShould = false;
			}

			if (node is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
			{
				_isShould = memberAccessExpressionSyntax.Name.Identifier.ValueText == "Should";
				if (_isShould)
				{
					if (_isConditional)
					{
						Subject = SyntaxFactory.ParseExpression(
							_subjectString + "?" + memberAccessExpressionSyntax.Expression);
						_isShould = false;
						_isConditional = false;
					}
				}
				else if (memberAccessExpressionSyntax.Parent is InvocationExpressionSyntax)
				{
					Methods.Push(new MethodDefinitionElement(new MethodDefinition(memberAccessExpressionSyntax)));
				}
				else if (memberAccessExpressionSyntax.Name.Identifier.ValueText == "And")
				{
					Methods.Push(new AndDefinitionElement());
				}
			}

			if (node is not ArgumentSyntax)
			{
				base.Visit(node);
			}
		}
	}
}

#pragma warning restore S1192
