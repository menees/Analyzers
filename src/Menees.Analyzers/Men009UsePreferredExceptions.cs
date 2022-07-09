namespace Menees.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men009UsePreferredExceptions : Analyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN009";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men009Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men009MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men009Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Usage, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		#endregion

		#region Public Methods

		public static string GetPreferredTypeName(string currentTypeName)
		{
			string result = null;

			switch (currentTypeName)
			{
				case "NotImplementedException":
					result = "NotSupportedException";
					break;
			}

			return result;
		}

		public override void Initialize(AnalysisContext context)
		{
			base.Initialize(context);
			context.RegisterSyntaxNodeActionHonorExclusions(this, HandleThrow, SyntaxKind.ThrowStatement, SyntaxKind.ThrowExpression);
		}

		#endregion

		#region Private Methods

		private static void HandleThrow(SyntaxNodeAnalysisContext context)
		{
			ObjectCreationExpressionSyntax creation = null;
			switch (context.Node)
			{
				case ThrowStatementSyntax statement:
					creation = statement.Expression as ObjectCreationExpressionSyntax;
					break;

				case ThrowExpressionSyntax expression:
					creation = expression.Expression as ObjectCreationExpressionSyntax;
					break;
			}

			if (creation != null)
			{
				switch (creation.Type.Kind())
				{
					case SyntaxKind.IdentifierName:
						IdentifierNameSyntax identifier = (IdentifierNameSyntax)creation.Type;
						HandleExceptionType(context, identifier);
						break;

					case SyntaxKind.QualifiedName:
						QualifiedNameSyntax qualifiedName = (QualifiedNameSyntax)creation.Type;
						if (qualifiedName.Right.IsKind(SyntaxKind.IdentifierName)
							&& string.Concat(qualifiedName.Left.DescendantTokens()) == "System")
						{
							HandleExceptionType(context, (IdentifierNameSyntax)qualifiedName.Right);
						}

						break;
				}
			}
		}

		private static void HandleExceptionType(SyntaxNodeAnalysisContext context, IdentifierNameSyntax simpleTypeName)
		{
			string currentType = simpleTypeName.Identifier.Text;
			string preferredType = GetPreferredTypeName(currentType);
			if (!string.IsNullOrEmpty(preferredType))
			{
				// The code fix provider depends on this returning the location of the IdentifierNameSyntax.
				Location location = simpleTypeName.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, preferredType, currentType));
			}
		}

		#endregion
	}
}
