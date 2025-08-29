namespace Menees.Analyzers;

public static class Rules
{
	#region Public Constants

	public const string Design = nameof(Design);

	public const string Layout = nameof(Layout);

	public const string Naming = nameof(Naming);

	public const string Spacing = nameof(Spacing);

	public const string Usage = nameof(Usage);

	// In debug builds everything will be enabled by default.
	// In release builds we'll disable rules that conflict with StyleCop by default.
	public const bool DisabledByDefault
#if DEBUG
		= true;
#else
		= false;
#endif

	public const bool EnabledByDefault = true;

	// In debug builds we'll treat Info like Warning so VS will show the wavy underlines for it,
	// so we can quickly tell if our diagnostic location information is correct.
	public const DiagnosticSeverity InfoSeverity
#if DEBUG
		= DiagnosticSeverity.Warning;
#else
		= DiagnosticSeverity.Info;
#endif

	#endregion

	#region Public Methods

	public static FileLinePositionSpan GetLineSpan(this SyntaxToken token)
		=> token.SyntaxTree?.GetLineSpan(token.Span) ?? default;

	public static FileLinePositionSpan GetLineSpan(this SyntaxTrivia trivia)
		=> trivia.SyntaxTree?.GetLineSpan(trivia.Span) ?? default;

	public static Tuple<string, Location> GetFileLocation(SyntaxTree tree, SourceText text, int startLine = 0, int endLine = 0)
	{
		Location location = Location.None;
		TextLineCollection lines = text.Lines;
		if (endLine >= startLine && lines.Count > endLine)
		{
			if (endLine == startLine)
			{
				location = Location.Create(tree, lines[startLine].Span);
			}
			else
			{
				TextSpan startSpan = lines[startLine].Span;
				TextSpan endSpan = lines[endLine].Span;
				location = Location.Create(tree, TextSpan.FromBounds(startSpan.Start, endSpan.End));
			}
		}

		string filePath = tree.FilePath;
		string fileName = !string.IsNullOrEmpty(filePath) ? Path.GetFileName(filePath) : string.Empty;
		return Tuple.Create(fileName, location);
	}

	public static Location GetFirstLineLocation(this SyntaxNode node)
	{
		Location result;

		SyntaxTree tree = node.SyntaxTree;
		if (tree.TryGetText(out SourceText? text))
		{
			// If the node starts with attribute list(s), then get the start of the first child that's not an attribute list.
			int nodeStart = node.SpanStart;
			ChildSyntaxList children = node.ChildNodesAndTokens();
			if (children[0].IsKind(SyntaxKind.AttributeList))
			{
				// There can be multiple attribute lists on a node, but a node must have at least one leaf token child.
				nodeStart = children.First(child => child.Kind() != SyntaxKind.AttributeList).SpanStart;
			}

			TextSpan lineSpan = text.Lines.GetLineFromPosition(nodeStart).Span;
			TextSpan blockFirstLineSpan = TextSpan.FromBounds(nodeStart, lineSpan.End);
			result = Location.Create(tree, blockFirstLineSpan);
		}
		else
		{
			// If we don't have source text, then "first line" isn't meaningful.
			result = node.GetLocation();
		}

		return result;
	}

	#endregion

	#region Internal Methods

	internal static void RegisterCodeBlockActionHonorExclusions(
		this AnalysisContext context,
		Analyzer analyzer,
		Action<CodeBlockAnalysisContext> action)
	{
		ConfigureStandardAnalysis(context);

		context.RegisterCodeBlockAction(
			c =>
			{
				ConfigureSettings(context, analyzer, c.Options, c.CancellationToken);
				SyntaxTree tree = c.CodeBlock.SyntaxTree;
				if (tree != null && !tree.IsGeneratedDocument(analyzer.Settings, c.CancellationToken))
				{
					action(c);
				}
			});
	}

	internal static void RegisterSyntaxTreeActionHonorExclusions(
		this AnalysisContext context,
		Analyzer analyzer,
		Action<SyntaxTreeAnalysisContext> action)
	{
		ConfigureStandardAnalysis(context);

		context.RegisterSyntaxTreeAction(
			c =>
			{
				ConfigureSettings(context, analyzer, c.Options, c.CancellationToken);
				if (!c.IsGeneratedDocument(analyzer.Settings))
				{
					action(c);
				}
			});
	}

	internal static void RegisterSyntaxNodeActionHonorExclusions(
		this AnalysisContext context,
		Analyzer analyzer,
		Action<SyntaxNodeAnalysisContext> action,
		params SyntaxKind[] syntaxKinds)
	{
		ConfigureStandardAnalysis(context);

		context.RegisterSyntaxNodeAction(
			c =>
			{
				ConfigureSettings(context, analyzer, c.Options, c.CancellationToken);
				SyntaxTree? tree = c.Node?.SyntaxTree;
				if (tree != null && !tree.IsGeneratedDocument(analyzer.Settings, c.CancellationToken))
				{
					action(c);
				}
			},
			ImmutableArray.Create(syntaxKinds));
	}

	internal static void RegisterSymbolActionHonorExclusions(
		this AnalysisContext context,
		Analyzer analyzer,
		Predicate<Compilation>? initialize,
		Action<SymbolAnalysisContext> action,
		params SymbolKind[] symbolKinds)
	{
		ConfigureStandardAnalysis(context);

		context.RegisterCompilationStartAction(
			compilationContext =>
			{
				if (initialize?.Invoke(compilationContext.Compilation) ?? true)
				{
					compilationContext.RegisterSymbolAction(
						c =>
						{
							ConfigureSettings(context, analyzer, c.Options, c.CancellationToken);
							if (c.Symbol.Locations.Any(location => location.SourceTree?.IsGeneratedDocument(analyzer.Settings, c.CancellationToken) is false))
							{
								action(c);
							}
						},
						ImmutableArray.Create(symbolKinds));
				}
			});
	}

	internal static bool HasIndicatorAttribute(this SyntaxList<AttributeListSyntax> attributeLists, ISet<string> attributeNames)
	{
		bool result = attributeLists.Any(list => list.Attributes.Any(
			attribute => (attribute.ArgumentList?.Arguments.Count ?? 0) == 0
			&& attribute.Name.IsKind(SyntaxKind.IdentifierName)
			&& attributeNames.Contains(((IdentifierNameSyntax)attribute.Name).Identifier.Text)));
		return result;
	}

	#endregion

	#region Private Methods

	private static void ConfigureStandardAnalysis(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
	}

	private static void ConfigureSettings(AnalysisContext context, Analyzer analyzer, AnalyzerOptions options, CancellationToken cancellationToken)
	{
		if (analyzer.Settings?.IsDefault ?? true)
		{
			analyzer.Settings = Settings.Cache(context, options, cancellationToken);
		}
	}

	#endregion
}
