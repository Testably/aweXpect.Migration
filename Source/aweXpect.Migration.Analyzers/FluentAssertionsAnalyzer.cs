using System.Collections.Immutable;
using aweXpect.Migration.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace aweXpect.Migration.Analyzers;

/// <summary>
///     An analyzer that flags most assertions from FluentAssertions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FluentAssertionsAnalyzer : DiagnosticAnalyzer
{
	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rules.FluentAssertionsRule,];

	/// <inheritdoc />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
	}

	private static void AnalyzeOperation(OperationAnalysisContext context)
	{
		if (context.Operation is IInvocationOperation invocationOperation)
		{
			IMethodSymbol? methodSymbol = invocationOperation.TargetMethod;

			string? fullyQualifiedNonGenericMethodName = methodSymbol.GloballyQualifiedNonGeneric();

			if (fullyQualifiedNonGenericMethodName.StartsWith("global::FluentAssertions") &&
			    fullyQualifiedNonGenericMethodName.EndsWith("Should"))
			{
				SyntaxNode syntax = invocationOperation.Syntax;
				while (syntax.Parent is ExpressionOrPatternSyntax && syntax.Parent is not AwaitExpressionSyntax)
				{
					syntax = syntax.Parent;
				}

				// Do not report nested `.Should()` e.g. in `.Should().AllSatisfy(x => x.Should().BeGreaterThan(0));`
				if (syntax.Parent is not ArgumentSyntax)
				{
					context.ReportDiagnostic(
						Diagnostic.Create(Rules.FluentAssertionsRule, syntax.GetLocation())
					);
				}
			}
		}
	}
}
