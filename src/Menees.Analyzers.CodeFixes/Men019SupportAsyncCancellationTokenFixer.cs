namespace Menees.Analyzers;

using System.ComponentModel;

#region Using Directives

using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;


#endregion

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men019SupportAsyncCancellationTokenFixer))]
public sealed class Men019SupportAsyncCancellationTokenFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics = ImmutableArray.Create(Men019SupportAsyncCancellationToken.DiagnosticId);

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
					Resources.Men019CodeFix,
					cancel => GetTransformedDocumentAsync(context.Document, diagnostic, cancel),
					nameof(Men019SupportAsyncCancellationTokenFixer)),
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
			SyntaxNode locationNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
			if (locationNode is MethodDeclarationSyntax method)
			{
				ParameterListSyntax oldParameterList = method.ParameterList;
				SeparatedSyntaxList<ParameterSyntax> separatedParameters = oldParameterList.Parameters;

				// Insert it before "ref", "out", "params", and optional parameters.
				// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1068#special-cases
				int insertIndex = separatedParameters.IndexOf(p => p.Default != null
					|| p.Modifiers.Any(token => token.Kind() is SyntaxKind.RefKeyword
						or SyntaxKind.OutKeyword
						or SyntaxKind.ParamsKeyword));
				if (insertIndex < 0)
				{
					insertIndex = separatedParameters.Count;
				}

				ParameterSyntax newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
					.WithType(SyntaxFactory.ParseTypeName(typeof(CancellationToken).FullName));
				separatedParameters = separatedParameters.Insert(insertIndex, newParameter);

				// https://www.meziantou.net/roslyn-annotations-for-code-fix.htm
				ParameterListSyntax newParameterList = SyntaxFactory.ParameterList(separatedParameters)
					.WithAdditionalAnnotations(Simplifier.Annotation)
					.WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
					.WithAdditionalAnnotations(Formatter.Annotation);

				SyntaxNode newSyntaxRoot = syntaxRoot.ReplaceNode(oldParameterList, newParameterList);
				result = document.WithSyntaxRoot(newSyntaxRoot);
			}
		}

		return result;
	}

	#endregion
}