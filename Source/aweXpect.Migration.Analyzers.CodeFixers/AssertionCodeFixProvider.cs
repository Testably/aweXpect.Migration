using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace aweXpect.Migration.Analyzers;

/// <summary>
///     Base class for code fix provider that migrates assertions to aweXpect.
/// </summary>
public abstract class AssertionCodeFixProvider(DiagnosticDescriptor rule) : CodeFixProvider
{
	/// <inheritdoc />
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = [rule.Id,];

	/// <inheritdoc />
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc />
	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		foreach (Diagnostic? diagnostic in context.Diagnostics)
		{
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			SyntaxNode? root =
				await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (root?.FindNode(diagnosticSpan) is ExpressionSyntax expressionSyntax
			    and (InvocationExpressionSyntax or ConditionalAccessExpressionSyntax or LambdaExpressionSyntax))
			{
				Document fixedDocument =
					await ConvertAssertionAsync(context, expressionSyntax, context.CancellationToken)
						.ConfigureAwait(false);
				if (fixedDocument == context.Document)
				{
					continue;
				}

				context.RegisterCodeFix(
					CodeAction.Create(
						rule.Title.ToString(),
						_ => Task.FromResult(fixedDocument),
						rule.Title.ToString()),
					diagnostic);
			}
		}
	}

	/// <summary>
	///     Converts the assertion.
	/// </summary>
	/// <remarks>
	///     Returns the unchanged <see cref="CodeFixContext.Document" /> when no faithful rewrite exists, so that no code fix
	///     is offered.
	/// </remarks>
	protected abstract Task<Document> ConvertAssertionAsync(CodeFixContext context,
		ExpressionSyntax expressionSyntax, CancellationToken cancellationToken);

	/// <summary>
	///     Adds a using directive for <paramref name="namespaceName" />, unless <paramref name="typeName" /> is already in
	///     scope at <paramref name="position" />.
	/// </summary>
	protected static async Task<CompilationUnitSyntax> AddUsingIfMissing(Document document,
		CompilationUnitSyntax compilationUnit, int position, string namespaceName, string typeName)
	{
		SemanticModel? semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
		if (semanticModel is null || !semanticModel.LookupNamespacesAndTypes(position, name: typeName).IsEmpty)
		{
			return compilationUnit;
		}

		string endOfLine = compilationUnit.ToFullString().Contains("\r\n") ? "\r\n" : "\n";
		UsingDirectiveSyntax usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
			.NormalizeWhitespace()
			.WithTrailingTrivia(SyntaxFactory.EndOfLine(endOfLine));
		UsingDirectiveSyntax? followingUsing = compilationUnit.Usings
			.FirstOrDefault(u => u.Name is not null && CompareUsings(u.Name.ToString(), namespaceName) > 0);
		return followingUsing is null
			? compilationUnit.AddUsings(usingDirective)
			: compilationUnit.InsertNodesBefore(followingUsing, [usingDirective,]);

		static int CompareUsings(string left, string right)
		{
			int systemOrder = IsSystem(right).CompareTo(IsSystem(left));
			return systemOrder != 0 ? systemOrder : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
		}

		static bool IsSystem(string name) => name == "System" || name.StartsWith("System.");
	}
}
