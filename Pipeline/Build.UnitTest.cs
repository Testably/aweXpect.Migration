using System.Linq;
using Fallout.Common;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable AllUnderscoreLocalParameterName

namespace Build;

partial class Build
{
	Target UnitTests => _ => _
		.DependsOn(DotNetUnitTests);

	Project[] UnitTestProjects =>
	[
		Solution.Tests.aweXpect_Migration_Tests,
	];

	/// <remarks>
	///     Each project and target framework is started with <c>dotnet run</c>, so that its TRX and coverage file get a
	///     name of their own.
	/// </remarks>
	Target DotNetUnitTests => _ => _
		.Unlisted()
		.DependsOn(Compile)
		.Executes(() =>
		{
			(Project Project, string Framework)[] testRuns = UnitTestProjects
				.SelectMany(project => (project.GetTargetFrameworks() ?? [])
					.Select(framework => (project, framework)))
				.ToArray();

			Assert.NotEmpty(testRuns);

			DotNetRun(s => s
					.SetConfiguration(Configuration)
					.SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
					.EnableNoBuild()
					.CombineWith(
						testRuns,
						(settings, testRun) => settings
							.SetProjectFile(testRun.Project)
							.SetFramework(testRun.Framework)
							.SetProcessAdditionalArguments(
								"--",
								"--disable-logo",
								$"--results-directory \"{TestResultsDirectory}\"",
								"--report-trx",
								$"--report-trx-filename {testRun.Project.Name}_{testRun.Framework}.trx",
								"--coverage",
								"--coverage-output-format cobertura",
								$"--coverage-output {testRun.Project.Name}_{testRun.Framework}.cobertura.xml")),
				completeOnFailure: true);
		});
}
