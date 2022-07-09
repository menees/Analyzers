namespace Menees.Analyzers
{
	#region Using Directives

	using static Menees.Analyzers.Men011AlignUsingDirectives;

	#endregion

	[Shared]
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men011AlignUsingDirectivesFixer))]
	public sealed class Men011AlignUsingDirectivesFixer : CodeFixProvider
	{
		#region Private Data Members

		private const string LevelProperty = "Level";

		private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men011AlignUsingDirectives.DiagnosticId);

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
						CodeFixes.Resources.Men011CodeFix,
						token => GetTransformedDocumentAsync(context.Document, diagnostic, token),
						equivalenceKey: nameof(Men011AlignUsingDirectivesFixer)),
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
			SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			Document result = ApplyFix(diagnostic, syntaxRoot, document);
			return result;
		}

		private static Document ApplyFix(Diagnostic diagnostic, SyntaxNode syntaxRoot, Document document)
		{
			Document result = document;

			if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan) is UsingDirectiveSyntax directive
				&& diagnostic.Properties.TryGetValue(LevelProperty, out string levelText) && int.TryParse(levelText, out int level))
			{
				IndentInfo indent = new(directive, level);
				result = ApplyFix(indent, syntaxRoot, result);
			}

			return result;
		}

		private static Document ApplyFix(IndentInfo indent, SyntaxNode syntaxRoot, Document document)
		{
			string newIndentText;

			Project project = document.Project;
			OptionSet options = project.Solution.Workspace.Options;
			int indentSize = options.GetOption(FormattingOptions.IndentationSize, project.Language);
			int newIndentTextLogicalLength = indentSize * indent.Level;
			if (options.GetOption(FormattingOptions.UseTabs, project.Language))
			{
				int tabSize = Math.Max(options.GetOption(FormattingOptions.TabSize, project.Language), 1);
				int numTabs = newIndentTextLogicalLength / tabSize;
				int numSpaces = newIndentTextLogicalLength % tabSize;
				newIndentText = new string('\t', numTabs) + new string(' ', numSpaces);
			}
			else
			{
				newIndentText = new string(' ', newIndentTextLogicalLength);
			}

			SyntaxNode newSyntaxRoot;
			if (indent.IndentTrivia == null)
			{
				// Insert leading indent trivia.
				SyntaxToken keyword = indent.Using.UsingKeyword;
				var newLeadingTrivia = keyword.LeadingTrivia.Concat(new[] { SyntaxFactory.Whitespace(newIndentText) });
				SyntaxToken newKeyword = keyword.WithLeadingTrivia(newLeadingTrivia);
				newSyntaxRoot = syntaxRoot.ReplaceToken(keyword, newKeyword);
			}
			else if (indent.Level == 0)
			{
				// Remove the invalid leading indent trivia.
				newSyntaxRoot = syntaxRoot.ReplaceTrivia(indent.IndentTrivia.Value, default(SyntaxTrivia));
			}
			else
			{
				// Replace the existing indent trivia with the correct indentation.
				SyntaxTrivia newIndentTrivia = SyntaxFactory.Whitespace(newIndentText);
				newSyntaxRoot = syntaxRoot.ReplaceTrivia(indent.IndentTrivia.Value, newIndentTrivia);
			}

			Document result = document.WithSyntaxRoot(newSyntaxRoot);
			return result;
		}

		#endregion
	}
}