# aweXpect.Migration

[![Nuget](https://img.shields.io/nuget/v/aweXpect.Migration)](https://www.nuget.org/packages/aweXpect.Migration)
[![Build](https://github.com/aweXpect/aweXpect.Migration/actions/workflows/build.yml/badge.svg)](https://github.com/aweXpect/aweXpect.Migration/actions/workflows/build.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Testably_aweXpect.Migration&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Testably_aweXpect.Migration)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Testably_aweXpect.Migration&metric=coverage)](https://sonarcloud.io/summary/overall?id=Testably_aweXpect.Migration)

We added support to migrate from other testing frameworks.

1. Temporarily install the
   `aweXpect.Migration` package in the test project and add the following global using statements in the test project:
   ```csharp
   global using System.Threading.Tasks;
   global using aweXpect;
   ```

2. Depending on the framework, the assertions will be marked with a warning:
	- For [FluentAssertions](https://fluentassertions.com/):  
      All usages of `.Should()` will be marked with
	  `aweXpectM002: fluentassertions should be migrated to aweXpect`
	- For [Xunit](https://xunit.net/):  
      All usages of `Assert` will be marked with `aweXpectM003: Xunit assertions should be migrated to aweXpect`

3. Most warnings can be automatically fixed with a code fix provider. Make sure to await all migrated expectations (fix `aweXpect0001: Expectations must be awaited or verified`).

4. Fix the remaining warnings manually.

5. Remove the `aweXpect.Migration` package again.
