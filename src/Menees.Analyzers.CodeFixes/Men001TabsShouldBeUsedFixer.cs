﻿namespace Menees.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men001TabsShouldBeUsedFixer))]
public sealed class Men001TabsShouldBeUsedFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men001TabsShouldBeUsed.DiagnosticId);

	#endregion

	#region Public Properties

	public sealed override ImmutableArray<string> FixableDiagnosticIds => FixableDiagnostics;

	#endregion

	#region Public Methods

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		foreach (Diagnostic diagnostic in context.Diagnostics.Where(d => FixableDiagnostics.Contains(d.Id)))
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					CodeFixes.Resources.Men001CodeFix,
					token => GetTransformedDocumentAsync(context.Document, diagnostic, token),
					equivalenceKey: nameof(Men001TabsShouldBeUsedFixer)),
				diagnostic);
		}

		return Task.FromResult(true);
	}

	#endregion

	#region Private Methods

	private static async Task<Document> GetTransformedDocumentAsync(
		Document document,
		Diagnostic diagnostic,
		CancellationToken cancellationToken)
	{
		Document result = document;

		SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (syntaxRoot != null)
		{
			// For DocumentationCommentExteriorTrivia we must use findInsideTrivia: true so we don't get the entire XML comment trivia.
			SyntaxTrivia violatingTrivia = syntaxRoot.FindTrivia(diagnostic.Location.SourceSpan.Start, findInsideTrivia: true);

			string originalIndent = violatingTrivia.ToFullString();
			int tabSize = GetTabSize(document);

			// If they've mixed tabs and spaces, then we need to figure out the original indent length in spaces.
			int startColumn = violatingTrivia.GetLineSpan().StartLinePosition.Character;
			int originalIndentColumn = startColumn;
			string? nonWhitespaceSuffix = null;
			for (int i = 0; i < originalIndent.Length; i++)
			{
				char ch = originalIndent[i];
				if (ch == '\t')
				{
					int offsetWithinTabColumn = originalIndentColumn % tabSize;
					int spaceCount = tabSize - offsetWithinTabColumn;
					originalIndentColumn += spaceCount;
				}
				else if (char.IsWhiteSpace(ch))
				{
					originalIndentColumn++;
				}
				else
				{
					// For DocumentationCommentExteriorTrivia we must keep the /// suffix.
					nonWhitespaceSuffix = originalIndent.Substring(i);
					break;
				}
			}

			// We know that the violating trivia was leading whitespace trivia, and we NEVER want to use
			// spaces for indentation.  So even if the indentation size isn't a multiple of the tab size, we'll
			// generate the new indentation string as only tabs.
			int originalIndentSpaceLength = originalIndentColumn - startColumn;
			int numTabs = originalIndentSpaceLength / tabSize;
			string tabIndent = new string('\t', numTabs) + nonWhitespaceSuffix;

			SyntaxTrivia newTrivia = violatingTrivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia)
				? SyntaxFactory.DocumentationCommentExterior(tabIndent)
				: SyntaxFactory.Whitespace(tabIndent);
			SyntaxNode newSyntaxRoot = syntaxRoot.ReplaceTrivia(violatingTrivia, newTrivia);
			result = document.WithSyntaxRoot(newSyntaxRoot);
		}

		return result;
	}

	private static int GetTabSize(Document document)
	{
		Project project = document.Project;
		OptionSet options = project.Solution.Workspace.Options;
		int result = options.GetOption(FormattingOptions.TabSize, project.Language);
		return result;
	}

	#endregion
}