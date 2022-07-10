namespace Menees.Analyzers
{
	#region Using Directives

	using Microsoft.CodeAnalysis.Rename;

	#endregion

	[Shared]
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men015UsePreferredTermsFixer))]
	public sealed class Men015UsePreferredTermsFixer : CodeFixProvider
	{
		#region Private Data Members

		private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men015UsePreferredTerms.DiagnosticId);

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
				if (diagnostic.Properties.TryGetValue(Men015UsePreferredTerms.PreferredKey, out string? preferredTerm)
					&& diagnostic.Properties.TryGetValue(Men015UsePreferredTerms.CanFixKey, out string? canFixText)
					&& bool.TryParse(canFixText, out bool canFix) && canFix && preferredTerm != null)
				{
					context.RegisterCodeFix(
						CodeAction.Create(
							CodeFixes.Resources.Men015CodeFix,
							cancel => GetTransformedSolutionAsync(context.Document, diagnostic, preferredTerm, cancel),
							nameof(Men015UsePreferredTermsFixer)),
						diagnostic);
				}
			}

			return Task.FromResult(true);
		}

		#endregion

		#region Private Methods

		private static async Task<Solution> GetTransformedSolutionAsync(
			Document document,
			Diagnostic diagnostic,
			string preferredTerm,
			CancellationToken cancellationToken)
		{
			// https://marcinjuraszek.com/2014/05/solution-wide-rename-from-code-fix-provider-fix-async-method-naming.html
			Solution result = document.Project.Solution;
			SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (syntaxRoot != null)
			{
				SyntaxToken violatingToken = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
				SyntaxNode? violatingNode = violatingToken.Parent;
				if (violatingToken.IsKind(SyntaxKind.IdentifierToken) && violatingNode != null)
				{
					SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
					if (semanticModel != null)
					{
						ISymbol? symbol = semanticModel.GetDeclaredSymbol(violatingNode, cancellationToken);
						if (symbol != null)
						{
							OptionSet optionSet = result.Workspace.Options;
							result = await Renamer.RenameSymbolAsync(result, symbol, preferredTerm, optionSet, cancellationToken);
						}
					}
				}
			}

			return result;
		}

		#endregion
	}
}