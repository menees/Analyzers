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

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	#endregion

	#region Public Methods

	public static string? GetPreferredText(string text)
	{
		string? preferredText = null;
		switch (text)
		{
			case "Now":
				preferredText = "UtcNow";
				break;

			case "Today":
				preferredText = "UtcNow.Date";
				break;
		}

		return preferredText;
	}

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
				// Check for DateTime, which could be fully-qualified, partially-qualified, or unqualified (e.g., via "using static").
				// I'm omitting DateTimeOffset.Now since it retains the local offset, and its comparisons use the UTC time.
				MemberAccessExpressionSyntax? memberAccess = identifier.Parent as MemberAccessExpressionSyntax;
				if ((memberAccess != null && memberAccess.Name == identifier && IsSystemDateTimeReference(memberAccess.Expression))
					|| ((memberAccess == null || memberAccess.Expression == identifier) && IsUsingStaticDateTimeReference(identifier.SyntaxTree)))
				{
					// The code fix provider depends on this returning the location of the IdentifierNameSyntax.
					Location location = identifier.GetLocation();
					ImmutableDictionary<string, string?> fixerProperties = GetProperties(identifier, preferredText);
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, fixerProperties, preferredText, text));
				}
			}
		}
	}

	private static bool IsSystemDateTimeReference(ExpressionSyntax expression)
	{
		bool result;

		switch (expression)
		{
			case MemberAccessExpressionSyntax memberAccess:
				result = memberAccess.Expression.ToString() == "System" && memberAccess.Name.ToString() == "DateTime";
				break;

			case IdentifierNameSyntax identifier:
				result = identifier.ToString() == "DateTime" && HasUsingDirective(identifier.SyntaxTree, "System");
				break;

			case QualifiedNameSyntax qualified:
				result = qualified.ToString() == "System.DateTime";
				break;

			default:
				result = false;
				break;
		}

		return result;
	}

	private static bool HasUsingDirective(SyntaxTree syntaxTree, string name)
	{
		IEnumerable<UsingDirectiveSyntax> usingDirectives = GetUsingDirectives(syntaxTree);
		bool result = usingDirectives.Any(directive => string.IsNullOrEmpty(directive.StaticKeyword.Text) && directive.Name.ToString() == name);
		return result;
	}

	private static bool IsUsingStaticDateTimeReference(SyntaxTree syntaxTree)
	{
		IEnumerable<UsingDirectiveSyntax> usingDirectives = GetUsingDirectives(syntaxTree);
		bool result = usingDirectives.Any(directive => !string.IsNullOrEmpty(directive.StaticKeyword.Text)
			&& IsSystemDateTimeReference(directive.Name));
		return result;
	}

	private static IEnumerable<UsingDirectiveSyntax> GetUsingDirectives(SyntaxTree syntaxTree)
	{
		return syntaxTree.GetRoot().DescendantNodes()
			.Where(node => node.IsKind(SyntaxKind.UsingDirective))
			.Select(directive => (UsingDirectiveSyntax)directive);
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
