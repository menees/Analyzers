namespace Menees.Analyzers.CodeFixes;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men017RemoveUnusedPrivateSetterFixer))]
public sealed class Men017RemoveUnusedPrivateSetterFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics
		= ImmutableArray.Create(Men017RemoveUnusedPrivateSetter.DiagnosticId);

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
					Resources.Men017CodeFix,
					cancellation => GetTransformedDocumentAsync(context.Document, diagnostic, cancellation),
					equivalenceKey: nameof(Men017RemoveUnusedPrivateSetterFixer)),
				diagnostic);
		}

		return Task.FromResult(true);
	}

	#endregion

	#region Private Methods

	private async Task<Document> GetTransformedDocumentAsync(
		Document document,
		Diagnostic diagnostic,
		CancellationToken cancellation)
	{
		Document result = document;

		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellation);
		if (root != null)
		{
			AccessorDeclarationSyntax? accessorDeclaration = root.FindNode(diagnostic.Location.SourceSpan)
				?.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
			AccessorListSyntax? accessorList = accessorDeclaration?.FirstAncestorOrSelf<AccessorListSyntax>();

			if (accessorList != null && accessorDeclaration != null)
			{
				AccessorListSyntax? newAccessorList = accessorList
					.RemoveNode(accessorDeclaration, SyntaxRemoveOptions.KeepExteriorTrivia)?
					.WithAdditionalAnnotations(Formatter.Annotation);
				if (newAccessorList != null)
				{
					SyntaxNode newRoot = root.ReplaceNode(accessorList, newAccessorList);
					result = document.WithSyntaxRoot(newRoot);
				}
			}
		}

		return result;
	}

	#endregion
}
