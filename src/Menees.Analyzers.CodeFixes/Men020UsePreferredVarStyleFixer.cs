namespace Menees.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Men020UsePreferredVarStyleFixer))]
public sealed class Men020UsePreferredVarStyleFixer : CodeFixProvider
{
	#region Private Data Members

	private static readonly ImmutableArray<string> FixableDiagnostics =
	[
		Men020UsePreferredVarStyle.DiagnosticIdBuiltIn,
		Men020UsePreferredVarStyle.DiagnosticIdSimple,
		Men020UsePreferredVarStyle.DiagnosticIdElsewhere,
	];

	private static readonly Dictionary<string, string> CodeFixTitles = new(StringComparer.Ordinal)
	{
		{ Men020UsePreferredVarStyle.DiagnosticIdBuiltIn, Resources.Men020BCodeFix },
		{ Men020UsePreferredVarStyle.DiagnosticIdSimple, Resources.Men020SCodeFix },
		{ Men020UsePreferredVarStyle.DiagnosticIdElsewhere, Resources.Men020ECodeFix },
	};

	private static readonly SymbolDisplayFormat NullableAwareMinimalFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
		.WithMiscellaneousOptions(
			SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions
			| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

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
			string title = CodeFixTitles.TryGetValue(diagnostic.Id, out string? t) ? t : diagnostic.Id;
			context.RegisterCodeFix(
				CodeAction.Create(
					title,
					cancel => GetTransformedDocumentAsync(context.Document, diagnostic, cancel),
					diagnostic.Id),
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
						TypeInfo typeInfo = model.GetTypeInfo(typeSyntax, cancellationToken);
						ITypeSymbol? typeSymbol = typeInfo.Type;

						// Adjust nullable annotation for reference types using flow state or initializer/contextual type analysis.
							if (typeSymbol != null
								&& typeSymbol.IsReferenceType
								&& typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
							{
								// Check the var keyword's flow state first (most reliable with Roslyn 5.3.0+).
								if (typeInfo.Nullability.FlowState == NullableFlowState.NotNull)
								{
									typeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
								}
								else
								{
									ExpressionSyntax? initializer = GetInitializerForType(typeSyntax);
									if (initializer != null)
									{
										TypeInfo initTypeInfo = model.GetTypeInfo(initializer, cancellationToken);
										if (initTypeInfo.Nullability.FlowState == NullableFlowState.NotNull)
										{
											typeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
										}
										else if (initTypeInfo.Type != null && initTypeInfo.Type.NullableAnnotation != NullableAnnotation.Annotated)
										{
											typeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
										}
									}
									else
									{
										ITypeSymbol? contextualType = GetContextualType(typeSyntax, model);
										if (contextualType != null && contextualType.NullableAnnotation != NullableAnnotation.Annotated)
										{
											typeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
										}
									}
								}
							}

						if (typeSymbol != null)
						{
							string typeName = typeSymbol.ToMinimalDisplayString(model, typeSyntax.SpanStart, NullableAwareMinimalFormat);
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

	private static ExpressionSyntax? GetInitializerForType(TypeSyntax typeSyntax)
	{
		// Navigate from the type syntax to find the initializer expression.
		// For local declarations: TypeSyntax → VariableDeclarationSyntax → VariableDeclaratorSyntax.Initializer.Value
		if (typeSyntax.Parent is VariableDeclarationSyntax declaration
			&& declaration.Variables.Count == 1)
		{
			return declaration.Variables[0].Initializer?.Value;
		}

		return null;
	}

	private static ITypeSymbol? GetContextualType(TypeSyntax typeSyntax, SemanticModel model)
	{
		// For foreach statements, use the element type from ForEachStatementInfo.
		if (typeSyntax.Parent is ForEachStatementSyntax forEach)
		{
			return model.GetForEachStatementInfo(forEach).ElementType;
		}

		// For declaration expressions (out var), use the parameter type.
		if (typeSyntax.Parent is DeclarationExpressionSyntax declExpr
			&& declExpr.Parent is ArgumentSyntax argument
			&& argument.Parent is BaseArgumentListSyntax argumentList
			&& argumentList.Parent is ExpressionSyntax containingExpression)
		{
			SymbolInfo symbolInfo = model.GetSymbolInfo(containingExpression);
			if (symbolInfo.Symbol is IMethodSymbol method)
			{
				if (argument.NameColon != null)
				{
					string paramName = argument.NameColon.Name.Identifier.Text;
					return method.Parameters.FirstOrDefault(p => p.Name == paramName)?.Type;
				}

				int argIndex = argumentList.Arguments.IndexOf(argument);
				if (argIndex >= 0 && argIndex < method.Parameters.Length)
				{
					return method.Parameters[argIndex].Type;
				}
			}
		}

		return null;
	}

	#endregion
}
