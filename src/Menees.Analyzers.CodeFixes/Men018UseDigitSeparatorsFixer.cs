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
			// Because we're finding the node by its location, this may return a parent node
			// (e.g., ArgumentSyntax that wraps the LiteralExpressionSyntax in Test(123456);).
			SyntaxNode locationNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
			LiteralExpressionSyntax? literalExpression = locationNode.DescendantNodesAndSelf()
				.OfType<LiteralExpressionSyntax>()
				.FirstOrDefault();
			if (literalExpression != null)
			{
				SyntaxToken literalToken = literalExpression.Token;
				SyntaxTriviaList lead = literalToken.LeadingTrivia;
				SyntaxTriviaList trail = literalToken.TrailingTrivia;
				SyntaxToken? newToken = Type.GetTypeCode(literalToken.Value?.GetType()) switch
				{
					TypeCode.SByte => SyntaxFactory.Literal(lead, preferredText, (sbyte)literalToken.Value!, trail),
					TypeCode.Byte => SyntaxFactory.Literal(lead, preferredText, (byte)literalToken.Value!, trail),
					TypeCode.Int16 => SyntaxFactory.Literal(lead, preferredText, (short)literalToken.Value!, trail),
					TypeCode.UInt16 => SyntaxFactory.Literal(lead, preferredText, (ushort)literalToken.Value!, trail),
					TypeCode.Int32 => SyntaxFactory.Literal(lead, preferredText, (int)literalToken.Value!, trail),
					TypeCode.UInt32 => SyntaxFactory.Literal(lead, preferredText, (uint)literalToken.Value!, trail),
					TypeCode.Int64 => SyntaxFactory.Literal(lead, preferredText, (long)literalToken.Value!, trail),
					TypeCode.UInt64 => SyntaxFactory.Literal(lead, preferredText, (ulong)literalToken.Value!, trail),
					TypeCode.Single => SyntaxFactory.Literal(lead, preferredText, (float)literalToken.Value!, trail),
					TypeCode.Double => SyntaxFactory.Literal(lead, preferredText, (double)literalToken.Value!, trail),
					TypeCode.Decimal => SyntaxFactory.Literal(lead, preferredText, (decimal)literalToken.Value!, trail),
					_ => null,
				};

				if (newToken != null)
				{
					SyntaxNode newNode = SyntaxFactory.LiteralExpression(
						SyntaxKind.NumericLiteralExpression,
						newToken.Value);

					SyntaxNode newSyntaxRoot = syntaxRoot.ReplaceNode(literalExpression, newNode);
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