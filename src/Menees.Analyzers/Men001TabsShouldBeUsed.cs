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
	public sealed class Men001TabsShouldBeUsed : DiagnosticAnalyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN001";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men001Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men001MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men001Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Rules.Spacing, DiagnosticSeverity.Warning, Rules.DisabledByDefault, Description);

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		#endregion

		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxTreeActionHonorExclusions(HandleSyntaxTree);
		}

		#endregion

		#region Private Methods

		private static void HandleWhitespaceTrivia(SyntaxTreeAnalysisContext context, SyntaxTrivia trivia)
		{
			// We can ignore structured trivia like directives (e.g., #region, #pragma) and XML comments.
			if (!trivia.HasStructure)
			{
				// We only need to look at leading trivia.  It's ok to use spaces between keywords (e.g., private void Method()),
				// and those usually show up as trailing trivia.  However, sometimes spaces between identifiers show up as leading
				// trivia (e.g., in <param name="X">).  So we have to make sure the leading trivia is really at the beginning of a line.
				SyntaxToken token = trivia.Token;
				if (token.LeadingTrivia.IndexOf(trivia) >= 0
					&& trivia.ToFullString().IndexOf(' ') >= 0
					&& trivia.GetLineSpan().StartLinePosition.Character == 0)
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, trivia.GetLocation()));
				}
			}
		}

		private static void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
			foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
			{
				switch (trivia.Kind())
				{
					case SyntaxKind.WhitespaceTrivia:
					case SyntaxKind.DocumentationCommentExteriorTrivia:
						HandleWhitespaceTrivia(context, trivia);
						break;
				}
			}
		}

		#endregion
	}
}
