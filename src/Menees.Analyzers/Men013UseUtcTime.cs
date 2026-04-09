namespace Menees.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men013UseUtcTime : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN013";

	public const string CanFixKey = "CanFix";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men013Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men013MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men013Description), Resources.ResourceManager, typeof(Resources));

	private static readonly DiagnosticDescriptor Rule =
		new(DiagnosticId, Title, MessageFormat, Rules.Usage, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

	#endregion

	#region Public Properties

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	#endregion

	#region Public Methods

	public static string? GetPreferredText(string text)
		=> text switch
		{
			"Now" => "UtcNow",
			"Today" => "UtcNow.Date",
			_ => null,
		};

	public override void Initialize(AnalysisContext context)
	{
		base.Initialize(context);
		context.RegisterSyntaxNodeActionHonorExclusions(this, HandleIdentifer, SyntaxKind.IdentifierName);
	}

	#endregion

	#region Private Methods

	private static void HandleIdentifer(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is IdentifierNameSyntax identifier)
		{
			string text = identifier.Identifier.Text;
			string? preferredText = GetPreferredText(text);
			if (preferredText != null)
			{
				// Use the semantic model to verify this is a System.DateTime property.
				// I'm omitting DateTimeOffset.Now since it retains the local offset, and its comparisons use the UTC time.
				SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken);
				if (symbolInfo.Symbol is IPropertySymbol propertySymbol
					&& propertySymbol.ContainingType?.SpecialType == SpecialType.System_DateTime)
				{
					// The code fix provider depends on this returning the location of the IdentifierNameSyntax.
					Location location = identifier.GetLocation();
					ImmutableDictionary<string, string?> fixerProperties = GetProperties(identifier, preferredText);
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, fixerProperties, preferredText, text));
				}
			}
		}
	}

	private static ImmutableDictionary<string, string?> GetProperties(IdentifierNameSyntax identifier, string preferredText)
	{
		// When "using static System.DateTime" is involved, then we can't always replace Today with UtcNow.Date.
		// See the InvalidCodeUsingStaticTest unit test for some examples of where we leave Today.
		bool canFix = preferredText.IndexOf('.') < 0
			|| (identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier);

		ImmutableDictionary<string, string?>.Builder builder = ImmutableDictionary.CreateBuilder<string, string?>();
		builder.Add(CanFixKey, canFix.ToString());
		return builder.ToImmutable();
	}

	#endregion
}
