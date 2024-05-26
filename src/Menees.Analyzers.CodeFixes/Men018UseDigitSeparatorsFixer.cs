namespace Menees.Analyzers;

#region Using Directives

using Microsoft.CodeAnalysis.Rename;

#endregion

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men018UseDigitSeparatorsFixer))]
public sealed class Men018UseDigitSeparatorsFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men018UseDigitSeparators.DiagnosticId);

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
			if (diagnostic.Properties.TryGetValue(Men018UseDigitSeparators.PreferredKey, out string? preferredText)
				&& !string.IsNullOrEmpty(preferredText))
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						CodeFixes.Resources.Men018CodeFix,
						cancel => GetTransformedDocumentAsync(context.Document, diagnostic, preferredText!, cancel),
						nameof(Men018UseDigitSeparatorsFixer)),
					diagnostic);
			}
		}

		return Task.FromResult(true);
	}

	#endregion

	#region Private Methods

	private static async Task<Document> GetTransformedDocumentAsync(
		Document document,
		Diagnostic diagnostic,
		string preferredText,
		CancellationToken cancellationToken)
	{
		Document result = document;

		SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (syntaxRoot != null)
		{
			SyntaxNode violatingNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
			if (violatingNode is LiteralExpressionSyntax literalExpression)
			{
				SyntaxToken literalToken = literalExpression.Token;

				SyntaxNode oldNode = literalExpression;
				SyntaxNode newNode = SyntaxFactory.LiteralExpression(
					SyntaxKind.NumericLiteralExpression,
					SyntaxFactory.Token(
						literalToken.LeadingTrivia,
						SyntaxKind.NumericLiteralToken,
						preferredText,
						preferredText,
						literalToken.TrailingTrivia));

				var newSyntaxRoot = syntaxRoot.ReplaceNode(oldNode, newNode);
				result = document.WithSyntaxRoot(newSyntaxRoot);
			}
		}

		return result;
	}

	private static IdentifierNameSyntax CreateIdentifier(SyntaxToken violatingToken, string newName)
	{
		SyntaxToken newToken = SyntaxFactory.Identifier(violatingToken.LeadingTrivia, newName, violatingToken.TrailingTrivia);
		IdentifierNameSyntax result = SyntaxFactory.IdentifierName(newToken);
		return result;
	}

	#endregion
}