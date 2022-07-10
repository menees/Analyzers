namespace Menees.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men005FileTooLong : Analyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN005";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men005Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men005MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men005Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Layout, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		#endregion

		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			base.Initialize(context);
			context.RegisterSyntaxTreeActionHonorExclusions(this, this.HandleSyntaxTree);
		}

		#endregion

		#region Private Methods

		private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			SyntaxTree tree = context.Tree;
			if (tree.TryGetText(out SourceText? text))
			{
				int maxLength = this.Settings.MaxFileLines;
				int fileLength = text.Lines.Count;
				if (fileLength > maxLength)
				{
					// Only mark the first line past the limit.  Marking every line from maxLength to fileLength-1 was ugly.
					var fileLocation = Rules.GetFileLocation(tree, text, maxLength, maxLength);
					context.ReportDiagnostic(Diagnostic.Create(Rule, fileLocation.Item2, fileLocation.Item1, maxLength, fileLength));
				}
			}
		}

		#endregion
	}
}