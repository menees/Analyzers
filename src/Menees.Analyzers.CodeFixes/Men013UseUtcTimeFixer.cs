namespace Menees.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men013UseUtcTimeFixer))]
public sealed class Men013UseUtcTimeFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men013UseUtcTime.DiagnosticId);

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
			if (diagnostic.Properties.TryGetValue(Men013UseUtcTime.CanFixKey, out string? canFixText)
				&& bool.TryParse(canFixText, out bool canFix) && canFix)
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						Resources.Men013CodeFix,
						cancel => GetTransformedDocumentAsync(context.Document, diagnostic, cancel),
						nameof(Men013UseUtcTimeFixer)),
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
				string? newName = Men013UseUtcTime.GetPreferredText(violatingToken.Text);

				if (!string.IsNullOrEmpty(newName) && newName != null)
				{
					SyntaxNode oldNode = violatingIdentifier;
					SyntaxNode newNode;

					int dotIndex = newName.IndexOf('.');
					if (dotIndex > 0)
					{
						string[] names = newName.Split('.');
						if (names.Length != 2)
						{
							throw new ArgumentException("Unsupported new name: " + newName);
						}

						switch (oldNode?.Parent)
						{
							case MemberAccessExpressionSyntax oldAccess:
								oldNode = oldAccess;
								newNode = SyntaxFactory.MemberAccessExpression(
									SyntaxKind.SimpleMemberAccessExpression,
									SyntaxFactory.MemberAccessExpression(
										SyntaxKind.SimpleMemberAccessExpression,
										oldAccess.Expression,
										SyntaxFactory.IdentifierName(names[0])),
									SyntaxFactory.Token(SyntaxKind.DotToken),
									CreateIdentifier(violatingToken, names[1]));
								break;

							default:
								// When "using static" is involved, we may not have a simple member access.
								// It's too hard to handle all possible cases where an identifier could be used
								// (e.g., either side of binary expressions, on left side of member access).
								throw new ArgumentException("Unsupported parent: " + oldNode?.Parent);
						}
					}
					else
					{
						newNode = CreateIdentifier(violatingToken, newName);
					}

					SyntaxNode newSyntaxRoot = syntaxRoot.ReplaceNode(oldNode, newNode);
					result = document.WithSyntaxRoot(newSyntaxRoot);
				}
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