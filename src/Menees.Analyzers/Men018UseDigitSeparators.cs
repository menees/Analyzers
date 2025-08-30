namespace Menees.Analyzers;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;

#endregion

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men018UseDigitSeparators : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN018";

	public const string PreferredKey = "Preferred";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men018Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men018MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men018Description), Resources.ResourceManager, typeof(Resources));

	private static readonly DiagnosticDescriptor Rule =
		new(DiagnosticId, Title, MessageFormat, Rules.Layout, DiagnosticSeverity.Info, Rules.EnabledByDefault, Description);

	#endregion

	#region Public Properties

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	#endregion

	#region Public Methods

	public override void Initialize(AnalysisContext context)
	{
		base.Initialize(context);
		context.RegisterSyntaxNodeActionHonorExclusions(this, HandleNumericLiteral, SyntaxKind.NumericLiteralExpression);
	}

	#endregion

	#region Private Methods

	private void HandleNumericLiteral(SyntaxNodeAnalysisContext context)
	{
		// Only make a recommendation if the literal contains no separators already.
		// If it's already separated in any way, we'll accept it.
		if (context.Node is LiteralExpressionSyntax literalExpression
			&& literalExpression.Token.IsKind(SyntaxKind.NumericLiteralToken)
			&& NumericLiteral.TryParse(literalExpression.Token.Text, out NumericLiteral? literal)
			&& literal.ScrubbedDigits == literal.OriginalDigits)
		{
			byte literalSize = literal.GetSize();
			(byte minSize, byte groupSize) = this.Settings.GetDigitSeparatorFormat(literal);
			if (literalSize >= minSize)
			{
				string literalText = literal.ToString();
				string preferredText = literal.ToString(groupSize);
				if (preferredText != literalText)
				{
					ImmutableDictionary<string, string?>.Builder builder = ImmutableDictionary.CreateBuilder<string, string?>();
					builder.Add(PreferredKey, preferredText);
					ImmutableDictionary<string, string?> fixerProperties = builder.ToImmutable();

					Location location = literalExpression.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, fixerProperties, literalText));
				}
			}
		}
	}

	#endregion
}
