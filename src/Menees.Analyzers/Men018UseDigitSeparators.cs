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
		if (context.Node is LiteralExpressionSyntax literalExpression
			&& literalExpression.Token.IsKind(SyntaxKind.NumericLiteralToken)
			&& NumericLiteral.TryParse(literalExpression.Token.Text, out NumericLiteral? literal)
			&& literal.ScrubbedDigits == literal.OriginalDigits)
		{
			string scrubbed = literal.ScrubbedDigits;
			switch (literal.Base)
			{
				case NumericBase.Hexadecimal:
					const int PreferredHexGroupSize = 2;
					CheckLiteral(scrubbed, PreferredHexGroupSize);
					break;

				case NumericBase.Binary:
					const int PreferredBinaryGroupSize = 4;
					CheckLiteral(scrubbed, PreferredBinaryGroupSize);
					break;

				default:
					const int PreferredDecimalGroupSize = 3;
					if (literal.IsInteger)
					{
						CheckLiteral(scrubbed, PreferredDecimalGroupSize);
					}
					else if (!scrubbed.Contains('e', StringComparison.OrdinalIgnoreCase))
					{
						// We have no exponent, and we only want to check the integer part.
						// Think about 123D, 123.0, .123, and 0.123.
						int decimalIndex = scrubbed.IndexOf('.');
						if (decimalIndex >= 0)
						{
							scrubbed = scrubbed.Substring(0, decimalIndex);
						}

						CheckLiteral(scrubbed, PreferredDecimalGroupSize);
					}

					break;
			}

			// TODO: Support code fixers too. [Bill, 5/25/2024]
			void CheckLiteral(string scrubbedDigits, int preferredGroupSize)
			{
				if (scrubbedDigits.Length > preferredGroupSize)
				{
					Location location = literalExpression.GetFirstLineLocation();
					string literalText = literalExpression.Token.Text;
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, literalText));
				}
			}
		}
	}

	#endregion
}
