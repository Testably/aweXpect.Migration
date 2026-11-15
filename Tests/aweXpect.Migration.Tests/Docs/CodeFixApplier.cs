using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using aweXpect.Migration.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace aweXpect.Migration.Tests.Docs;

/// <summary>
///     Applies the code fix of a diagnostic to a document in the setup of <see cref="SnippetCompiler" />.
/// </summary>
internal static class CodeFixApplier
{
	private const int MaxFixes = 50;

	private static readonly ImmutableArray<DiagnosticAnalyzer> Analyzers =
		[new FluentAssertionsAnalyzer(), new XunitAssertionAnalyzer(),];

	private static readonly CodeFixProvider[] Fixers =
		[new FluentAssertionsCodeFixProvider(), new XunitAssertionCodeFixProvider(),];

	/// <summary>
	///     Applies the code fix for the <paramref name="diagnosticId" /> to the <paramref name="source" /> until no
	///     diagnostic with a code fix is left, and returns the resulting source.
	/// </summary>
	public static async Task<string> Apply(string diagnosticId, string source)
	{
		using AdhocWorkspace workspace = new();
		ProjectId projectId = ProjectId.CreateNewId();
		DocumentId documentId = DocumentId.CreateNewId(projectId);
		Solution solution = workspace.CurrentSolution
			.AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Snippet", "Snippet", LanguageNames.CSharp,
				compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
					nullableContextOptions: NullableContextOptions.Enable),
				parseOptions: SnippetCompiler.ParseOptions,
				metadataReferences: await SnippetCompiler.References.Value))
			.AddDocument(DocumentId.CreateNewId(projectId), "ImplicitUsings.g.cs", SnippetCompiler.ImplicitUsings)
			.AddDocument(documentId, "Snippet.cs", source);
		CodeFixProvider fixer = Fixers.Single(provider => provider.FixableDiagnosticIds.Contains(diagnosticId));
		Document document = solution.GetDocument(documentId)!;
		for (int i = 0; i < MaxFixes; i++)
		{
			Document? fixedDocument = await FixFirstDiagnostic(document, diagnosticId, fixer);
			if (fixedDocument is null)
			{
				break;
			}

			document = fixedDocument;
		}

		return (await document.GetTextAsync()).ToString();
	}

	private static async Task<Document?> FixFirstDiagnostic(Document document, string diagnosticId,
		CodeFixProvider fixer)
	{
		Compilation compilation = (await document.Project.GetCompilationAsync())!;
		SyntaxTree tree = (await document.GetSyntaxTreeAsync())!;
		ImmutableArray<Diagnostic> diagnostics =
			await compilation.WithAnalyzers(Analyzers).GetAnalyzerDiagnosticsAsync();
		foreach (Diagnostic diagnostic in diagnostics
			         .Where(diagnostic => diagnostic.Id == diagnosticId && diagnostic.Location.SourceTree == tree)
			         .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start))
		{
			List<CodeAction> actions = [];
			await fixer.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic,
				(action, _) => actions.Add(action), CancellationToken.None));
			if (actions.Count == 0)
			{
				continue;
			}

			ImmutableArray<CodeActionOperation> operations = await actions[0].GetOperationsAsync(CancellationToken.None);
			return operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id);
		}

		return null;
	}
}
