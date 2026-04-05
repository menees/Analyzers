namespace Menees.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men020UsePreferredVarStyleFixer))]
public sealed class Men020UsePreferredVarStyleFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men020UsePreferredVarStyle.DiagnosticId);

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
					Resources.Men020CodeFix,
					cancel => GetTransformedDocumentAsync(context.Document, diagnostic, cancel),
					nameof(Men020UsePreferredVarStyleFixer)),
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
			SyntaxNode node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
			if (node is TypeSyntax typeSyntax)
			{
				if (typeSyntax.IsVar)
				{
					SemanticModel? model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
					if (model != null)
					{
						ITypeSymbol? typeSymbol = model.GetTypeInfo(typeSyntax, cancellationToken).Type;
						if (typeSymbol != null)
						{
							string typeName = typeSymbol.ToMinimalDisplayString(model, typeSyntax.SpanStart);
							TypeSyntax newTypeSyntax = SyntaxFactory.ParseTypeName(typeName)
								.WithLeadingTrivia(typeSyntax.GetLeadingTrivia())
								.WithTrailingTrivia(typeSyntax.GetTrailingTrivia());
							SyntaxNode newRoot = syntaxRoot.ReplaceNode(typeSyntax, newTypeSyntax);
							result = document.WithSyntaxRoot(newRoot);
						}
					}
				}
				else
				{
					TypeSyntax newTypeSyntax = SyntaxFactory.IdentifierName("var")
						.WithLeadingTrivia(typeSyntax.GetLeadingTrivia())
						.WithTrailingTrivia(typeSyntax.GetTrailingTrivia());
					SyntaxNode newRoot = syntaxRoot.ReplaceNode(typeSyntax, newTypeSyntax);
					result = document.WithSyntaxRoot(newRoot);
				}
			}
		}

		return result;
	}

	#endregion
}
