namespace Menees.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men012FlagsShouldBePowersOfTwo : Analyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN012";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men012Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men012MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormatNoValue =
			new LocalizableResourceString(nameof(Resources.Men012MessageFormatNoValue), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men012Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Layout, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		private static readonly DiagnosticDescriptor RuleNoValue =
			new(DiagnosticId, Title, MessageFormatNoValue, Rules.Layout, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		private static readonly HashSet<string> FlagsAttributeNames = ["Flags", "FlagsAttribute"];

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleNoValue);

		#endregion

		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			base.Initialize(context);
			context.RegisterSyntaxNodeActionHonorExclusions(this, HandleEnum, SyntaxKind.EnumDeclaration);
		}

		#endregion

		#region Private Methods

		private static void HandleEnum(SyntaxNodeAnalysisContext context)
		{
			SyntaxNode block = context.Node;
			SyntaxKind blockKind = block.Kind();

			if (blockKind == SyntaxKind.EnumDeclaration)
			{
				EnumDeclarationSyntax enumSyntax = (EnumDeclarationSyntax)block;
				if (enumSyntax.AttributeLists.HasIndicatorAttribute(FlagsAttributeNames))
				{
					foreach (EnumMemberDeclarationSyntax member in enumSyntax.Members)
					{
						ExpressionSyntax? valueExpression = member.EqualsValue?.Value;
						if (valueExpression == null)
						{
							Location location = member.GetFirstLineLocation();
							string enumName = enumSyntax.Identifier.Text;
							context.ReportDiagnostic(
								Diagnostic.Create(RuleNoValue, location, enumName, member.Identifier.Text));
						}
						else
						{
							valueExpression = GetInnerExpression(valueExpression);

							SyntaxKind expressionKind = valueExpression.Kind();
							if (expressionKind == SyntaxKind.NumericLiteralExpression)
							{
								LiteralExpressionSyntax literalValueExpression = (LiteralExpressionSyntax)valueExpression;
								SyntaxToken literal = literalValueExpression.Token;
								string valueText = literal.Text;
								if (Settings.TryParseIntegerLiteral(valueText, out ulong value) && value > 0 && !IsPowerOfTwo(value))
								{
									Location location = member.GetFirstLineLocation();
									string enumName = enumSyntax.Identifier.Text;
									context.ReportDiagnostic(
										Diagnostic.Create(Rule, location, enumName, member.Identifier.Text, valueText.Trim(), string.Empty));
								}
							}
							else if (expressionKind != SyntaxKind.BitwiseOrExpression)
							{
								// Any other expression type is suspicious and should be warned about even if it calculates to a power of two.
								// Only literal powers of two should be needed for Flags enums.
								Location location = member.GetFirstLineLocation();
								string enumName = enumSyntax.Identifier.Text;
								string expressionText = valueExpression.GetText().ToString().Trim();
								context.ReportDiagnostic(
									Diagnostic.Create(Rule, location, enumName, member.Identifier.Text, expressionText, "literal "));
							}
						}
					}
				}
			}
		}

		private static ExpressionSyntax GetInnerExpression(ExpressionSyntax valueExpression)
		{
			do
			{
				// Note: We can only skip expression wrappers that don't change the expression value.
				// So we can't skip unary minus, bitwise not, or other pre/postfix operators.
				SyntaxKind kind = valueExpression.Kind();
				if (kind == SyntaxKind.ParenthesizedExpression)
				{
					valueExpression = ((ParenthesizedExpressionSyntax)valueExpression).Expression;
				}
				else if (kind == SyntaxKind.UnaryPlusExpression)
				{
					valueExpression = ((PrefixUnaryExpressionSyntax)valueExpression).Operand;
				}
				else
				{
					break;
				}
			}
			while (true);

			return valueExpression;
		}

		private static bool IsPowerOfTwo(ulong value)
		{
			// This cleverness came from http://www.skorks.com/2010/10/write-a-function-to-determine-if-a-number-is-a-power-of-2/.
			// A power of two only has one bit on, and the value one less than that power of two has all right bits on up to that point.
			// So ANDing those two values together will give zero, and it should only happen if the original value was a power of two.
			// Also at http://www.graphics.stanford.edu/~seander/bithacks.html#DetermineIfPowerOf2.
			bool result = value != 0 && (value & (value - 1)) == 0;
			return result;
		}

		#endregion
	}
}
