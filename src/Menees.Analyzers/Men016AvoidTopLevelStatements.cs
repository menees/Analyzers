namespace Menees.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men016AvoidTopLevelStatements : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN016";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men016Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men016MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men016Description), Resources.ResourceManager, typeof(Resources));

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
		context.RegisterSyntaxNodeActionHonorExclusions(this, HandleGlobalStatement, SyntaxKind.GlobalStatement);
	}

	#endregion

	#region Private Methods

	private static void HandleGlobalStatement(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is GlobalStatementSyntax statement)
		{
			Location location = statement.GetLocation();
			context.ReportDiagnostic(Diagnostic.Create(Rule, location));
		}
	}

	#endregion
}
