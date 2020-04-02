namespace Menees.Analyzers
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.CodeAnalysis.Text;

	#endregion

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men005FileTooLong : DiagnosticAnalyzer
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
			new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Rules.Layout, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		private Settings settings;

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		#endregion

		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterCompilationStartAction(startContext => { this.settings = Settings.Cache(startContext); });
			context.RegisterSyntaxTreeActionHonorExclusions(this.HandleSyntaxTree);
		}

		#endregion

		#region Private Methods

		private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			SyntaxTree tree = context.Tree;
			if (tree.TryGetText(out SourceText text))
			{
				int maxLength = this.settings.MaxFileLines;
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