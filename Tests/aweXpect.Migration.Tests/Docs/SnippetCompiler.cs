using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;

namespace aweXpect.Migration.Tests.Docs;

/// <summary>
///     Compiles the code blocks of a page against aweXpect and the packages that the code fix tests use for
///     FluentAssertions and xUnit.
/// </summary>
/// <remarks>
///     The statements of each block become the body of an async method. Its types and <c>using</c> directives are
///     shared with the other blocks of the page, so that what a page shows once applies to all of its blocks, like it
///     does for a reader. The usings of a test project with implicit usings and of the migrated libraries apply to
///     every block. A <c>#line</c> directive maps
///     every part back to the page, and an initializer that is only a <c>// ...</c> comment becomes <c>default!</c>.
/// </remarks>
internal static partial class SnippetCompiler
{
	internal const string ImplicitUsings = """
		global using System;
		global using System.Collections.Generic;
		global using System.IO;
		global using System.Linq;
		global using System.Net.Http;
		global using System.Threading;
		global using System.Threading.Tasks;
		global using aweXpect;
		global using FluentAssertions;
		global using Xunit;
		global using static aweXpect.Expect;
		""";

	/// <summary>
	///     A block may declare its types before the statements that use them, as the wrapper separates both.
	/// </summary>
	private const string StatementsAfterTypes = "CS8803";

	internal static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);
	internal static readonly Lazy<Task<MetadataReference[]>> References = new(GetReferences);

	/// <summary>
	///     Returns the compiler errors of the <paramref name="blocks" />, each prefixed with the page and line.
	/// </summary>
	/// <remarks>
	///     A block with syntax errors is reported on its own and not wrapped, as a stray token would otherwise move into
	///     the generated code and cause errors there that do not point to the page.
	/// </remarks>
	public static async Task<List<string>> GetErrors(IReadOnlyList<CodeBlock> blocks)
	{
		List<string> errors = [];
		List<SyntaxTree> trees = [CSharpSyntaxTree.ParseText(ImplicitUsings, ParseOptions, "ImplicitUsings.g.cs"),];
		for (int i = 0; i < blocks.Count; i++)
		{
			CodeBlock block = blocks[i];
			CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(
					OmittedInitializer().Replace(block.Code, "= default!; //..."), ParseOptions)
				.GetCompilationUnitRoot();
			List<string> syntaxErrors = root.GetDiagnostics()
				.Where(diagnostic => IsError(diagnostic) && diagnostic.Id != StatementsAfterTypes)
				.Select(diagnostic => Format(diagnostic, block.Page, block.Line))
				.ToList();
			if (syntaxErrors.Count > 0)
			{
				errors.AddRange(syntaxErrors);
				continue;
			}

			trees.Add(CSharpSyntaxTree.ParseText(Wrap(block, root, i), ParseOptions, $"Block{i}.g.cs"));
		}

		CSharpCompilation compilation = CSharpCompilation.Create("Snippets", trees, await References.Value,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));
		errors.AddRange(compilation.GetDiagnostics()
			.Where(IsError)
			.Select(diagnostic => Format(diagnostic, null, 1)));
		return errors;
	}

	/// <remarks>
	///     A <c>using</c> directive becomes global, with <c>global</c> on a line of its own, so that the directive keeps
	///     the columns it has on the page.
	/// </remarks>
	private static string Wrap(CodeBlock block, CompilationUnitSyntax root, int index)
	{
		StringBuilder source = new();
		foreach (UsingDirectiveSyntax directive in root.Usings)
		{
			if (directive.GlobalKeyword.IsKind(SyntaxKind.None))
			{
				source.AppendLine("global");
			}

			AppendMapped(source, block, [directive,]);
		}

		source.AppendLine("#line default")
			.AppendLine($"public class Block{index}")
			.AppendLine("{")
			.AppendLine("public async Task Run()")
			.AppendLine("{");
		AppendMapped(source, block, root.Members.OfType<GlobalStatementSyntax>());
		source.AppendLine("#line default").AppendLine("}").AppendLine("}");
		AppendMapped(source, block, root.Members.Where(member => member is not GlobalStatementSyntax));
		return source.ToString();
	}

	/// <summary>
	///     Wraps the <paramref name="code" /> of a block like <see cref="GetErrors" /> does, but as a document of its own
	///     and without mapping it back to the page, so that two blocks wrap to the same text where their code is the same.
	/// </summary>
	/// <remarks>
	///     Blank lines before a statement or type are dropped, as a fixer that adds a <c>using</c> directive moves the
	///     blank line after the directives into the generated method.
	/// </remarks>
	public static string WrapAsDocument(string code)
	{
		CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(
				OmittedInitializer().Replace(code, "= default!; //..."), ParseOptions)
			.GetCompilationUnitRoot();
		StringBuilder source = new();
		foreach (UsingDirectiveSyntax directive in root.Usings)
		{
			source.AppendLine(directive.ToFullString().Trim('\r', '\n'));
		}

		source.AppendLine("public class Snippet").AppendLine("{").AppendLine("public async Task Run()").AppendLine("{");
		AppendTrimmed(source, root.Members.OfType<GlobalStatementSyntax>());
		source.AppendLine("}").AppendLine("}");
		AppendTrimmed(source, root.Members.Where(member => member is not GlobalStatementSyntax));
		return source.ToString();

		static void AppendTrimmed(StringBuilder source, IEnumerable<SyntaxNode> nodes)
		{
			foreach (SyntaxNode node in nodes)
			{
				source.AppendLine(node.ToFullString().Trim('\r', '\n'));
			}
		}
	}

	private static void AppendMapped(StringBuilder source, CodeBlock block, IEnumerable<SyntaxNode> nodes)
	{
		foreach (SyntaxNode node in nodes)
		{
			int line = block.Line + node.SyntaxTree.GetLineSpan(node.FullSpan).StartLinePosition.Line;
			source.AppendLine($"#line {line} \"{block.Page}\"").AppendLine(node.ToFullString());
		}
	}

	private static bool IsError(Diagnostic diagnostic)
		=> diagnostic.Severity == DiagnosticSeverity.Error;

	private static string Format(Diagnostic diagnostic, string? page, int firstLine)
	{
		FileLinePositionSpan span = diagnostic.Location.GetMappedLineSpan();
		return $"{page ?? span.Path}({span.StartLinePosition.Line + firstLine},{span.StartLinePosition.Character + 1}): " +
		       $"{diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}";
	}

	private static async Task<MetadataReference[]> GetReferences()
	{
		ImmutableArray<MetadataReference> references = await ReferenceAssemblies.Net.Net100
			.AddPackages([
				new PackageIdentity("xunit.v3", "1.1.0"),
				new PackageIdentity("FluentAssertions", "8.2.0"),
			])
			.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
		return
		[
			..references,
			MetadataReference.CreateFromFile(typeof(Expect).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(ThatBool).Assembly.Location),
		];
	}

	[GeneratedRegex(@"=\s*//\s*\.\.\.")]
	private static partial Regex OmittedInitializer();
}
