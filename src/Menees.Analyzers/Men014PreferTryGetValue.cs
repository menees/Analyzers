namespace Menees.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men014PreferTryGetValue : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN014";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men014Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men014MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men014Description), Resources.ResourceManager, typeof(Resources));

	private static readonly DiagnosticDescriptor Rule =
		new(DiagnosticId, Title, MessageFormat, Rules.Usage, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

	#endregion

	#region Public Properties

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	#endregion

	#region Public Methods

	public override void Initialize(AnalysisContext context)
	{
		base.Initialize(context);
		context.RegisterSyntaxNodeActionHonorExclusions(this, HandleIdentifer, SyntaxKind.IdentifierName);
	}

	#endregion

	#region Private Methods

	private static void HandleIdentifer(SyntaxNodeAnalysisContext context)
	{
		// See https://github.com/dotnet/runtime/issues/33798 for a similar proposed runtime rule.
		if (context.Node is IdentifierNameSyntax identifier)
		{
			string text = identifier.Identifier.Text;
			if (text == "ContainsKey"
				&& identifier.Parent is MemberAccessExpressionSyntax memberAccess
				&& memberAccess.Parent is InvocationExpressionSyntax invocation
				&& invocation.ArgumentList.Arguments.Count == 1)
			{
				// Instead of requiring invocation.Parent to be IfStatementSyntax, we'll be more permissive.
				// We'll search the whole containing block, and we'll require the indexer call to be after the ContainsKey call.
				BlockSyntax searchBlock = invocation.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
				if (searchBlock != null)
				{
					ExpressionSyntax dictionary = memberAccess.Expression;
					ArgumentSyntax keyArg = invocation.ArgumentList.Arguments[0];
					foreach (ElementAccessExpressionSyntax indexer in searchBlock.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
					{
						if (indexer.Expression.IsEquivalentTo(dictionary)
							&& indexer.ArgumentList.Arguments.Count == 1
							&& indexer.ArgumentList.Arguments[0].IsEquivalentTo(keyArg)
							&& indexer.SpanStart > invocation.SpanStart
							&& indexer.Parent is not AssignmentExpressionSyntax)
						{
							Location location = identifier.GetLocation();

							// It's a lot of work to try to determine the out data type depending on whether the dictionary
							// is a local variable, member variable, parameter, member property, etc. So I'll just use var.
							string preferred = $"{dictionary}.TryGetValue({keyArg}, out var value)";
							context.ReportDiagnostic(
								Diagnostic.Create(Rule, location, preferred, invocation.ToString(), indexer.ToString()));
						}
					}
				}
			}
		}
	}

	#endregion
}
