using System.Collections.Generic;
using System.IO;
using System.Linq;
using static aweXpect.Expect;

namespace aweXpect.Migration.Tests.Docs;

public sealed class DocsPagesTests
{
	private static readonly string PagesDirectory = GetPagesDirectory();

	public static IEnumerable<string> Pages()
		=> Directory.EnumerateFiles(PagesDirectory, "*.md", SearchOption.AllDirectories)
			.Select(path => Path.GetRelativePath(PagesDirectory, path).Replace('\\', '/'))
			.Order(StringComparer.Ordinal);

	[Test]
	[MethodDataSource(nameof(Pages))]
	public async Task CodeBlocks_ShouldCompile(string page)
	{
		List<string> errors = await SnippetCompiler.GetErrors(ExtractCodeBlocks(page));

		Fail.Unless(errors.Count == 0, string.Join(Environment.NewLine, errors));
	}

	[Test]
	public async Task Extract_ShouldFindTheCodeBlocksOfAllPages()
	{
		int count = Pages().Sum(page => ExtractCodeBlocks(page).Count);

		await That(count).IsGreaterThan(25)
			.Because("the check must not pass because it no longer finds the code blocks");
	}

	[Test]
	public async Task Extract_ShouldOnlyReturnCSharpBlocksWithoutNoCompileMarker()
	{
		string[] lines =
		[
			"# Title",
			"```csharp",
			"int a = 1;",
			"```",
			"```csharp no-compile",
			"int b = 2;",
			"```",
			"- In a list:",
			"  ```csharp title=\"Before\"",
			"  int c = 3;",
			"  ```",
			"\t```\tcsharp",
			"\tint d = 4;",
			"\t```",
			"````markdown",
			"```csharp",
			"int e = 5;",
			"```",
			"````",
			"```xml",
			"<f />",
			"```",
		];

		List<CodeBlock> blocks = CodeBlock.Extract("page.md", lines);

		await That(blocks).IsEqualTo([
			new CodeBlock("page.md", 3, "int a = 1;"),
			new CodeBlock("page.md", 10, "  int c = 3;"),
			new CodeBlock("page.md", 13, "\tint d = 4;"),
		]);
	}

	[Test]
	public async Task GetErrors_ShouldAllowTypesBeforeStatements()
	{
		CodeBlock block = new("page.md", 1, "class Album { public string Title { get; set; } = \"\"; }\n\nAlbum album = new();\nawait That(album.Title).IsEmpty();");

		List<string> errors = await SnippetCompiler.GetErrors([block,]);

		await That(errors).IsEmpty();
	}

	[Test]
	public async Task GetErrors_ShouldReportSyntaxErrorsOfABlockOnTheirOwn()
	{
		CodeBlock block = new("Docs/pages/page.md", 10, "int value = 1;\n}");

		List<string> errors = await SnippetCompiler.GetErrors([block,]);

		await That(errors).HasSingle().Which.StartsWith("Docs/pages/page.md(11,1): CS1022:")
			.Because("the stray brace would otherwise close the generated method and cause errors in the generated code");
	}

	[Test]
	public async Task GetErrors_ShouldReportThePageAndLineOfTheBlock()
	{
		CodeBlock block = new("Docs/pages/page.md", 10, "int value = 1;\nawait Expect.That(value).IsFoo();");

		List<string> errors = await SnippetCompiler.GetErrors([block,]);

		await That(errors).HasSingle().Which.StartsWith("Docs/pages/page.md(11,26): CS1061:");
	}

	[Test]
	public async Task GetErrors_ShouldShareTypesAndUsingsBetweenTheBlocksOfAPage()
	{
		CodeBlock first = new("page.md", 1, "using System.Text;\n\nclass Album { public string Title { get; set; } = \"\"; }");
		CodeBlock second = new("page.md", 5, "Album album = // ...\nalbum.Title.Should().Be(new StringBuilder(\"Abbey Road\").ToString());");
		CodeBlock third = new("page.md", 9, "Album album = new() { Title = \"Abbey Road\" };\nAssert.Equal(\"Abbey Road\", album.Title);");

		List<string> errors = await SnippetCompiler.GetErrors([first, second, third,]);

		await That(errors).IsEmpty()
			.Because("an initializer that is only a `// ...` comment becomes `default!` and each block has its own locals");
	}

	private static List<CodeBlock> ExtractCodeBlocks(string page)
		=> CodeBlock.Extract($"Docs/pages/{page}", File.ReadAllLines(Path.Combine(PagesDirectory, page)));

	private static string GetPagesDirectory()
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);
		while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Docs", "pages")))
		{
			directory = directory.Parent;
		}

		return directory is null
			? throw new DirectoryNotFoundException($"No Docs/pages above {AppContext.BaseDirectory}")
			: Path.Combine(directory.FullName, "Docs", "pages");
	}
}
