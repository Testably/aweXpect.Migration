---
title: Migration
---

[![Nuget](https://img.shields.io/nuget/v/aweXpect.Migration?label=aweXpect.Migration&logo=nuget)](https://www.nuget.org/packages/aweXpect.Migration)

`aweXpect.Migration` is a Roslyn analyzer that migrates assertions from [FluentAssertions](https://fluentassertions.com/)
and [xUnit](https://xunit.net/) to aweXpect. It flags each assertion it can migrate, and its code fix rewrites the call
site to the aweXpect equivalent.

{README}
