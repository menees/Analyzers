namespace Menees.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men009UsePreferredExceptionsFixer))]
public sealed class Men009UsePreferredExceptionsFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men009UsePreferredExceptions.DiagnosticId);

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
					Resources.Men009CodeFix,
					token => GetTransformedDocumentAsync(context.Document, diagnostic, token),
					equivalenceKey: nameof(Men009UsePreferredExceptionsFixer)),
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
			SyntaxNode violatingNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
			if (violatingNode is IdentifierNameSyntax violatingIdentifier)
			{
				SyntaxToken violatingToken = violatingIdentifier.Identifier;
				string? newName = Men009UsePreferredExceptions.GetPreferredTypeName(violatingToken.Text);
				if (!string.IsNullOrEmpty(newName) && newName != null)
				{
					SyntaxToken newToken = SyntaxFactory.Identifier(violatingToken.LeadingTrivia, newName, violatingToken.TrailingTrivia);
					IdentifierNameSyntax newIdentifier = SyntaxFactory.IdentifierName(newToken);

					SyntaxNode newSyntaxRoot = syntaxRoot.ReplaceNode(violatingIdentifier, newIdentifier);
					result = document.WithSyntaxRoot(newSyntaxRoot);
				}
			}
		}

		return result;
	}

	#endregion
}