using System.Collections.Immutable;
using aweXpect.Migration.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace aweXpect.Migration.Analyzers;

/// <summary>
///     An analyzer that flags most xunit assertions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XunitAssertionAnalyzer : DiagnosticAnalyzer
{
	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(Rules.XunitAssertionRule);

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

			if (fullyQualifiedNonGenericMethodName.StartsWith("global::Xunit.Assert."))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(Rules.XunitAssertionRule, context.Operation.Syntax.GetLocation())
				);
			}
		}
	}
}
