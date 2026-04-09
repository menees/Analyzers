![windows build & test](https://github.com/menees/Analyzers/workflows/windows%20build%20&%20test/badge.svg) [![Nuget](https://img.shields.io/nuget/v/Menees.Analyzers)](https://www.nuget.org/packages/Menees.Analyzers/)

# Analyzers 

This project provides several new C# code analysis rules for projects built with the .NET 10 SDK or later. These rules can be used with other analyzers like [\.NET's](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview).

This software is CharityWare. If you use it, I ask that you donate something to the charity of your choice.

| ID  | Title | Comment |
| ------ | ----------------------------------- | --- |
| MEN001 | Tabs should be used for indentation | Ensures tabs are used for indentation instead of spaces. This is the _opposite_ of StyleCop's [SA1027: TabsMustNotBeUsed](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1027.md) rule. This is similar to the StyleCop+ rule [SP2001: CheckAllowedIndentationCharacters](https://stylecopplus.codeplex.com/#MoreCustomRules) when set to "Tabs only".<br><br>This rule is off by default because it conflicts with Visual Studio's default settings, which use spaces instead of tabs for indentation. This rule can be enabled using a custom ruleset file, and it includes a code fix provider.  |
| MEN002 | Line is too long  | Ensures that lines are not longer than 160 characters. This is similar to the StyleCop+ rule [SP2100: CodeLineMustNotBeLongerThan](https://stylecopplus.codeplex.com/#MoreCustomRules). The MaxLineColumns and TabSize values for this rule can be configured in Menees.Analyzers.Settings.xml. |
| MEN002A | Line is long  | Notifies when lines are longer than 160 characters. This rule is off by default because MEN002 takes precedence and is sufficient for most cases. If you enable this rule, then you should also configure NotifyLineColumns to be less than MaxLineColumns in Menees.Analyzers.Settings.xml. |
| MEN003 | Method is too long | Ensures that methods are not longer than 120 lines. This is similar to the StyleCop+ rule [SP2101: MethodMustNotContainMoreLinesThan](https://stylecopplus.codeplex.com/#MoreCustomRules). The MaxMethodLines limit for this rule can be configured in Menees.Analyzers.Settings.xml.  |
| MEN004 | Property accessor is too long | Ensures that property accessors are not longer than 80 lines. This is similar to the StyleCop+ rule [SP2102: PropertyMustNotContainMoreLinesThan](https://stylecopplus.codeplex.com/#MoreCustomRules). The MaxPropertyAccessorLines limit for this rule can be configured in Menees.Analyzers.Settings.xml. |
| MEN005 | File is too long  | Ensures that files are not longer than 2000 lines. This is similar to the StyleCop+ rule [SP2103: FileMustNotContainMoreLinesThan](https://stylecopplus.codeplex.com/#MoreCustomRules). The MaxFileLines limit for this rule can be configured in Menees.Analyzers.Settings.xml. |
| MEN006 | #regions should be used | Recommends that #regions be used when there are over 100 lines in a file or if there is more than one class, struct, enum, or interface defined in a file.<br><br>This rule is off by default because it conflicts with StyleCop's [SA1124: DoNotUseRegions](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1124.md) rule. This rule can be enabled using a custom ruleset file. The MaxUnregionedLines limit for this rule can be configured in Menees.Analyzers.Settings.xml.<br><br>Note: [Menees VS Tools](https://github.com/menees/VsTools) can be used to easily add, collapse, and expand #regions in C# code (as well as in VB, XML, XAML, HTML, SQL, JavaScript, and TypeScript code). |
| MEN007 | Use a single return  | Recommends that only a single return statement should be used in a code block. "_One entry, one exit_" keeps control flow simple and makes refactoring easier. |
| MEN008 | File name should match type | Ensures that a file name matches or includes the name of the main type it contains. The TypeFileNameExclusions for this rule can be configured in Menees.Analyzers.Settings.xml. |
| MEN009 | Use the preferred exception type | Recommends preferred exception types (e.g., [NotSupportedException instead of NotImplementedException](http://blogs.msdn.com/b/brada/archive/2004/07/29/201354.aspx)). This rule is a complement to [CA2201](https://msdn.microsoft.com/library/ms182338.aspx), and it includes a code fix provider. |
| MEN010 | Avoid magic numbers  | Recommends that named constants be used instead of numeric literals (i.e., [magic numbers](https://en.wikipedia.org/wiki/Magic_number_(programming)#Accepted_limited_use_of_magic_numbers)). The AllowedNumericLiterals for this rule can be configured in Menees.Analyzers.Settings.xml. |
| MEN011 | Align using directives  | Ensures that using directives are aligned.  This is important when using directives are nested inside a namespace (per [SA1200: UsingDirectivesMustBePlacedWithinNamespace](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1200.md)) because Visual Studio will often fail to indent them correctly if they're added while code contains syntax errors.  This rule includes a code fix provider. |
| MEN012 | Flags should be powers of two | Flags enum members should be powers of two or bitwise-or combinations of named members. This rule is a complement to [CA2217](https://msdn.microsoft.com/en-us/library/ms182335.aspx). |
| MEN013 | Use UTC time | Recommends UTC times because they're unambiguous and always increasing. This rule includes a code fix provider.  |
| MEN014 | Prefer TryGetValue | Recommends calling TryGetValue (for a single lookup and retrieval) instead of ContainsKey and this[key] with duplicate lookups. |
| MEN015 | Use Preferred Terms | Similar to the old FxCop [CA1726 rule](https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1726?view=vs-2019#rule-description) except this rule only checks single terms (not double terms). So it uses a slightly different set of [default preferred terms](https://github.com/menees/Analyzers/blob/172bf0b6820e143de28f2f43e712908179ca1073/src/Menees.Analyzers/Settings.cs#L53-L77) (omitting double terms like LogOn), and it includes Id as a preferred term over ID (per FxCop's [CA1709 rule](https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1709?view=vs-2019#rule-description)). |
| MEN016 | Avoid Top-Level Statements | C# [top-level statements](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/top-level-statements) are only for toy/example programs and should be avoided in long-term code for [consistency and maintainability](https://github.com/dotnet/docs/issues/27420#issuecomment-988776134). |
| MEN017 | Remove Unused Private Setter | A private set accessor is not needed when an auto property is only assigned in the constructor. Inspired by [C# Essentials' Use Getter-Only Auto-Property](https://github.com/DustinCampbell/CSharpEssentials?tab=readme-ov-file#use-getter-only-auto-property). |
| MEN018 | Use Digit Separators | Numeric literals should use digit separators ('_' from C# 7) to improve readability. This applies to hexadecimal, binary, and [integer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types#integer-literals) or [real](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types#real-literals) literals. |
| MEN019 | Async method needs CancellationToken | An async method should take a [CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) parameter or take a parameter that has a public CancellationToken property. Inspired by [this dotnet issue](https://github.com/dotnet/runtime/issues/78397) and related to [CA2016](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2016) and [CA1068](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1068#special-cases). |
| MEN020B | Use preferred var style for built-in types | Local variable declarations for built-in types (e.g., `int`, `string`, `bool`) should use the preferred `var` style based on the configured BuiltInTypes rules. The BuiltInTypes category can contain a `<UseExplicitType />` or `<UseVar>` child element. `<UseVar>` can optionally include condition children (Foreach, LinqScalarResult, LinqCollectionResult, LinqAggregateResult, LongTypeName, Evident) to only allow `var` when a condition is met. This rule includes a code fix provider and is related to [IDE0007 and IDE0008](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0007-ide0008). |
| MEN020S | Use preferred var style for simple types | Local variable declarations for simple types (non-generic, non-built-in types like `Guid`, `MyClass`) should use the preferred `var` style based on the configured SimpleTypes rules. This rule includes a code fix provider and is related to [IDE0007 and IDE0008](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0007-ide0008). |
| MEN020E | Use preferred var style elsewhere | Local variable declarations for other types (e.g., generic types like `List<int>`, `Dictionary<string, int>`) should use the preferred `var` style based on the configured Elsewhere rules. This rule includes a code fix provider and is related to [IDE0007 and IDE0008](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0007-ide0008). |

## Configuration

Many of the rule limits and settings can be configured via `.editorconfig` or an XML settings file.
When both are present, `.editorconfig` values take precedence over XML values, which take precedence over defaults.

### .editorconfig

Add `menees_analyzers.*` keys to your `.editorconfig` file:

```ini
[*.cs]
menees_analyzers.max_line_columns = 120
menees_analyzers.tab_size = 4
menees_analyzers.allow_long_uri_lines = true
menees_analyzers.preferred_term.Cancelled = Canceled
menees_analyzers.var_style.built_in_types = use_explicit_type
```

#### Supported keys

| Category | Key | Type | Default |
|---|---|---|---|
| Scalars | `menees_analyzers.tab_size` | int | `4` |
| | `menees_analyzers.max_line_columns` | int | `160` |
| | `menees_analyzers.notify_line_columns` | int | `160` |
| | `menees_analyzers.max_method_lines` | int | `120` |
| | `menees_analyzers.max_property_accessor_lines` | int | `80` |
| | `menees_analyzers.max_file_lines` | int | `2000` |
| | `menees_analyzers.max_unregioned_lines` | int | `100` |
| | `menees_analyzers.allow_long_uri_lines` | bool | `true` |
| | `menees_analyzers.allow_long_four_slash_comment_lines` | bool | `false` |
| Cancellation | `menees_analyzers.cancellation.check_private_methods` | bool | `false` |
| | `menees_analyzers.cancellation.check_private_types` | bool | `false` |
| | `menees_analyzers.cancellation.property_names` | comma-delimited | `CancellationToken, Cancellation` |
| Digit Separators | `menees_analyzers.decimal.min_size` | byte | `6` |
| | `menees_analyzers.decimal.group_size` | byte | `3` |
| | `menees_analyzers.hexadecimal.min_size` | byte | `8` |
| | `menees_analyzers.hexadecimal.group_size` | byte | `4` |
| | `menees_analyzers.binary.min_size` | byte | `8` |
| | `menees_analyzers.binary.group_size` | byte | `4` |
| Collections | `menees_analyzers.allowed_numeric_literals` | comma-delimited | `0, 1, 2, 100` |
| | `menees_analyzers.allowed_numeric_caller_names` | comma-delimited | `FromDays, FromHours, ...` |
| | `menees_analyzers.allowed_numeric_caller_regexes` | comma-delimited | _(none)_ |
| | `menees_analyzers.test_class_attributes` | comma-delimited | `TestClass, TestFixture` |
| | `menees_analyzers.test_method_attributes` | comma-delimited | `TestMethod, Test, Fact, Theory` |
| File Exclusions | `menees_analyzers.analyze_file_name_exclusions` | comma-delimited | `GeneratedCode.cs` |
| | `menees_analyzers.analyze_file_regex_exclusions` | comma-delimited | _(see [source](src/Menees.Analyzers/Settings.cs))_ |
| | `menees_analyzers.type_file_name_exclusions` | comma-delimited | `Enumerations.cs, Interfaces.cs, Delegates.cs` |
| | `menees_analyzers.type_file_regex_exclusions` | comma-delimited | _(see [source](src/Menees.Analyzers/Settings.cs))_ |
| Preferred Terms | `menees_analyzers.preferred_term.<Avoid>` | string (Prefer) | _(merged with [defaults](src/Menees.Analyzers/Settings.cs))_ |
| Var Style | `menees_analyzers.var_style.built_in_types` | `use_explicit_type` \| `use_var` | _(none)_ |
| | `menees_analyzers.var_style.simple_types` | `use_explicit_type` \| `use_var` | _(none)_ |
| | `menees_analyzers.var_style.elsewhere` | `use_explicit_type` \| `use_var` | _(none)_ |
| Var Conditions | `menees_analyzers.var_style.<category>.foreach` | bool | |
| | `menees_analyzers.var_style.<category>.linq_scalar_result` | bool | |
| | `menees_analyzers.var_style.<category>.linq_collection_result` | bool | |
| | `menees_analyzers.var_style.<category>.linq_aggregate_result` | bool | |
| | `menees_analyzers.var_style.<category>.long_type_name` | bool | |
| | `menees_analyzers.var_style.<category>.long_type_name_length` | int | `30` |
| | `menees_analyzers.var_style.<category>.evident` | bool | |

**Notes:**
- Comma-delimited collections (e.g., `allowed_numeric_literals`) **replace** the defaults entirely when specified.
- Preferred terms **merge** with the [defaults](src/Menees.Analyzers/Settings.cs): `.editorconfig` terms add to or override the default preferred terms.
- Var style conditions (`foreach`, `evident`, etc.) apply only when the mode is `use_var`. The `<category>` placeholder is `built_in_types`, `simple_types`, or `elsewhere`.

### XML settings file

For backward compatibility, settings can also be configured using a `Menees.Analyzers.Settings.xml` file, which must comply with the `Menees.Analyzers.Settings.xsd` schema. The schema and `Settings.cs` file are available in the [Menees.Analyzers source code](src/Menees.Analyzers). A project using Menees.Analyzers with a custom XML settings file should set the Build Action for its `Menees.Analyzers.Settings.xml` file to "C# analyzer additional file".
