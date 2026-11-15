using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace aweXpect.Migration.Tests.Docs;

/// <summary>
///     An <c>&lt;ApplyFix&gt;</c> box of a documentation page, which starts at the one-based <paramref name="Line" />
///     and shows the code before and after the code fix for the diagnostic <paramref name="Id" /> as its
///     <c>```csharp</c> <paramref name="Blocks" />, without the highlight comments.
/// </summary>
internal sealed partial record ApplyFixBox(string Page, int Line, string Id, IReadOnlyList<string> Blocks)
{
	/// <summary>
	///     Extracts the <c>&lt;ApplyFix&gt;</c> boxes from the <paramref name="lines" /> of the <paramref name="page" />.
	/// </summary>
	public static List<ApplyFixBox> Extract(string page, string[] lines)
	{
		List<ApplyFixBox> boxes = [];
		for (int i = 0; i < lines.Length; i++)
		{
			Match opening = Opening().Match(lines[i]);
			if (!opening.Success)
			{
				continue;
			}

			int end = Array.FindIndex(lines, i, line => line.Trim() == "</ApplyFix>");
			if (end < 0)
			{
				end = lines.Length;
			}

			List<string> blocks = CodeBlock.Extract(page, lines[i..end], includeNoCompile: true)
				.Select(block => string.Join("\n", block.Code.Split('\n').Where(line => !HighlightComment().IsMatch(line))))
				.ToList();
			boxes.Add(new ApplyFixBox(page, i + 1, opening.Groups["id"].Value, blocks));
			i = end;
		}

		return boxes;
	}

	[GeneratedRegex("""^\s*<ApplyFix\s+id="(?<id>[^"]+)".*>\s*$""")]
	private static partial Regex Opening();

	[GeneratedRegex(@"^\s*//\s*highlight-(next-line|start|end)\s*$")]
	private static partial Regex HighlightComment();
}
