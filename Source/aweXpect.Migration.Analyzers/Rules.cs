using Microsoft.CodeAnalysis;

namespace aweXpect.Migration.Analyzers;

/// <summary>
///     Diagnostic rules reported by the aweXpect migration analyzers.
/// </summary>
public static class Rules
{
	private const string UsageCategory = "Usage";

	/// <summary>
	///     Rule <c>aweXpectM002</c>: a FluentAssertions assertion was detected and can be migrated to aweXpect.
	/// </summary>
	public static readonly DiagnosticDescriptor FluentAssertionsRule =
		CreateDescriptor("aweXpectM002", UsageCategory, DiagnosticSeverity.Warning);

	/// <summary>
	///     Rule <c>aweXpectM003</c>: an xUnit assertion was detected and can be migrated to aweXpect.
	/// </summary>
	public static readonly DiagnosticDescriptor XunitAssertionRule =
		CreateDescriptor("aweXpectM003", UsageCategory, DiagnosticSeverity.Warning);


	private static DiagnosticDescriptor CreateDescriptor(string diagnosticId, string category,
		DiagnosticSeverity severity) => new(
		diagnosticId,
		new LocalizableResourceString(diagnosticId + "Title",
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(diagnosticId + "MessageFormat", Resources.ResourceManager,
			typeof(Resources)),
		category,
		severity,
		true,
		new LocalizableResourceString(diagnosticId + "Description", Resources.ResourceManager,
			typeof(Resources))
	);
}
