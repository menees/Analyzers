namespace Menees.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men004PropertyAccessorTooLong : Analyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN004";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men004Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men004MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men004Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Layout, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		private static readonly HashSet<SyntaxKind> SupportedSyntaxKinds = new()
		{
			SyntaxKind.GetAccessorDeclaration,
			SyntaxKind.SetAccessorDeclaration,
			SyntaxKind.AddAccessorDeclaration,
			SyntaxKind.RemoveAccessorDeclaration,
		};

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		#endregion

		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			base.Initialize(context);
			context.RegisterCodeBlockActionHonorExclusions(this, this.HandleAccessor);
		}

		#endregion

		#region Private Methods

		private void HandleAccessor(CodeBlockAnalysisContext context)
		{
			Men003MethodTooLong.HandleBlockTooLong(context, SupportedSyntaxKinds, Rule, this.Settings.MaxPropertyAccessorLines);
		}

		#endregion
	}
}
