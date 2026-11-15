using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace aweXpect.Migration.Tests.Docs;

/// <summary>
///     A <c>```csharp</c> code block of a documentation page, whose code starts at the one-based
///     <paramref name="Line" />.
/// </summary>
internal sealed partial record CodeBlock(string Page, int Line, string Code)
{
	/// <summary>
	///     The info-string marker that excludes a block from the compile check.
	/// </summary>
	public const string NoCompile = "no-compile";

	/// <summary>
	///     Extracts the <c>```csharp</c> blocks from the <paramref name="lines" /> of the <paramref name="page" />,
	///     except the ones marked with <see cref="NoCompile" /> unless <paramref name="includeNoCompile" /> is set.
	/// </summary>
	/// <remarks>
	///     As in CommonMark, only a line of at least as many fence characters closes a block, so a longer fence can show
	///     a fenced block inside of it.
	/// </remarks>
	public static List<CodeBlock> Extract(string page, string[] lines, bool includeNoCompile = false)
	{
		List<CodeBlock> blocks = [];
		int i = 0;
		while (i < lines.Length)
		{
			Match opening = Fence().Match(lines[i++]);
			if (!opening.Success)
			{
				continue;
			}

			string fence = opening.Groups["fence"].Value;
			int start = i;
			while (i < lines.Length && !IsClosing(lines[i], fence))
			{
				i++;
			}

			string[] info = opening.Groups["info"].Value.Split([' ', '\t',], StringSplitOptions.RemoveEmptyEntries);
			if (info is ["csharp", ..,] && (includeNoCompile || !info.Contains(NoCompile)))
			{
				blocks.Add(new CodeBlock(page, start + 1, string.Join("\n", lines, start, i - start)));
			}

			i++;
		}

		return blocks;
	}

	private static bool IsClosing(string line, string fence)
	{
		string trimmed = line.Trim();
		return trimmed.Length >= fence.Length && trimmed.All(c => c == fence[0]);
	}

	[GeneratedRegex(@"^\s*(?<fence>`{3,}|~{3,})(?<info>.*)$")]
	private static partial Regex Fence();
}
