namespace Menees.Analyzers
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Threading;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.CodeAnalysis.Text;

	#endregion

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men002LineTooLong : DiagnosticAnalyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN002";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men002Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men002MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men002Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Layout, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

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

		private static int GetLineDisplayLength(string text, int tabSize, int maxLength, out int maxIndex)
		{
			const int NonIndex = -1;
			maxIndex = NonIndex;

			// Note: This code was copied from HighlightClassifier.CheckExcessLength in Menees VS Tools.
			//
			// Even if tabs are normally converted to spaces, we can still encounter tabs that we need to expand
			// (e.g., if they load a file that contains tabs).  So we have to scan the whole line to calculate
			// the correct visible length (i.e., column-based) based on tabs expanded to the next tab stop.
			int result = 0;
			int textLength = text.Length;
			int textIndex;
			for (textIndex = 0; textIndex < textLength; textIndex++)
			{
				if (text[textIndex] == '\t')
				{
					// A tab always takes up at least one column and up to TabSize columns.
					// We just need to add the number of columns to get to the next tab stop.
					result += tabSize - (result % tabSize); // Always in [1, TabSize] range.
				}
				else
				{
					result++;
				}

				if (result > maxLength && maxIndex == NonIndex)
				{
					maxIndex = textIndex;
				}
			}

			return result;
		}

		private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			if (context.Tree.TryGetText(out SourceText text))
			{
				int tabSize = this.settings.TabSize;
				int maxLength = this.settings.MaxLineColumns;

				foreach (TextLine line in text.Lines)
				{
					// We only need to calculate the line's full display length (i.e., with tabs expanded to the next tab stops)
					// if the display length could possibly be greater than the limit.  If every character in the line is full tab size
					// and it's still an acceptable length, then we can skip calling TextLine.ToString() and doing the expensive calc.
					TextSpan lineSpan = line.Span;
					if (lineSpan.Length * tabSize > maxLength)
					{
						int displayLength = GetLineDisplayLength(line.ToString(), tabSize, maxLength, out int maxIndex);
						if (displayLength > maxLength)
						{
							TextSpan excess = TextSpan.FromBounds(lineSpan.Start + maxIndex, lineSpan.End);
							Location location = Location.Create(context.Tree, excess);
							context.ReportDiagnostic(Diagnostic.Create(Rule, location, maxLength, displayLength));
						}
					}
				}
			}
		}

		#endregion
	}
}