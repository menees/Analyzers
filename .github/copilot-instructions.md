# Copilot Instructions for C# Code

## General

* Use tabs for indentation (not spaces), with a tab size of 4.
* Keep lines to a maximum of 160 characters.
* Keep methods to a maximum of 120 lines.
* Keep property accessors to a maximum of 80 lines.
* Keep files to a maximum of 2000 lines.
* Use file-scoped namespace declarations.
* Use `#region` / `#endregion` blocks to organize code sections (e.g., Private Data Members, Constructors, Public Methods).
* Enable nullable reference types (`#nullable enable` or via project settings).
* Use implicit usings where configured by the project.
* Use the latest C# language version features.
* Treat warnings as errors.

## Naming

* Use PascalCase for types, methods, properties, and events.
* Prefix interfaces with `I`.
* Use `Id` instead of `ID`.
* Use `Canceled` instead of `Cancelled`.
* Follow the preferred terms defined in the project's analyzer settings (e.g., `AreNot` instead of `Arent`, `Cannot` instead of `Cant`).

## Style

* Place `using` directives inside the namespace, wrapped in a `#region Using Directives` block.
* Prefer braces for control flow statements.
* Do not use expression-bodied members for methods, constructors, or operators.
* Use expression-bodied members for properties, indexers, accessors, and lambdas.
* Use named constants instead of magic numbers.
* Use digit separators (`_`) in large numeric literals for readability.
* Async methods should accept a `CancellationToken` parameter.
* Avoid top-level statements in production code.
* Use a single return statement per method.

## Project Structure

* This is a Roslyn analyzer project targeting .NET Standard 2.0 (for the analyzer) and .NET 10 / .NET Framework 4.8 (for tests and VSIX).
* Assemblies are strong-name signed.
* Configuration is supported via `.editorconfig` (preferred) and `Menees.Analyzers.Settings.xml`.
