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

	private static void HandleNumericLiteral(SyntaxNodeAnalysisContext context)
	{
		// Only make a recommendation if the literal contains no separators already.
		// If it's already separated in any way, we'll accept it.
		if (context.Node is LiteralExpressionSyntax literalExpression
			&& literalExpression.Token.IsKind(SyntaxKind.NumericLiteralToken)
			&& NumericLiteral.TryParse(literalExpression.Token.Text, out NumericLiteral? literal)
			&& literal.ScrubbedDigits == literal.OriginalDigits)
		{
			const byte PreferredHexGroupSize = 2; // Per-Byte
			const byte PreferredBinaryGroupSize = 4; // Per-Nibble
			const byte PreferredDecimalGroupSize = 3; // Per-Thousand
			byte preferredGroupSize = literal.Base switch
			{
				NumericBase.Hexadecimal => PreferredHexGroupSize,
				NumericBase.Binary => PreferredBinaryGroupSize,
				_ => PreferredDecimalGroupSize,
			};

			// For integers, this length check is a quick short-circuit.
			// For reals, it may be insufficient (e.g., 12.5 is 4 chars,
			// but the integer part is only 2). However, comparing the ToString
			// results below will be sufficient to avoid false positives.
			if (literal.ScrubbedDigits.Length > preferredGroupSize)
			{
				string literalText = literal.ToString();
				string preferredText = literal.ToString(preferredGroupSize);
				if (preferredText != literalText)
				{
					var builder = ImmutableDictionary.CreateBuilder<string, string?>();
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
