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
	public sealed class Men011AlignUsingDirectives : DiagnosticAnalyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN011";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men011Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men011MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men011Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Rules.Spacing, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

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

		private static void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
			var usingInfo = root.DescendantNodesAndSelf()
				.Where(node => node.IsKind(SyntaxKind.UsingDirective))
				.Select(directive => new
				{
					Using = (UsingDirectiveSyntax)directive,
					Level = directive.Ancestors()
						.Count(ancestor => ancestor.IsKind(SyntaxKind.NamespaceDeclaration)),
				});

			foreach (var levelGroup in usingInfo.GroupBy(info => info.Level))
			{
				var indentInfos = levelGroup.Select(info => new IndentInfo(info.Using, info.Level));

				// If there were any usings where we couldn't determine the indent validity immediately,
				// then we'll only assume its valid if everything in that sub-group uses the same indent.
				// This should only happen when spaces are used for indentation of usings in a namespace.
				bool defaultValidity = false;
				var unknownValidity = indentInfos.Where(indent => indent.IsValid == null);
				if (unknownValidity.Any())
				{
					// We need to ignore the known valid or invalid cases in order for our unit tests to
					// succeed when mixing tabs and space indents.  Tabs can always be validated, but
					// space indents can't.  So in a mixed case, we only want to check whether all the
					// space indents are consistent with each other at a specific level.
					defaultValidity = unknownValidity.Select(indent => indent.IndentText).Distinct().Count() == 1;
				}

				var properties = new Dictionary<string, string>() { { "Level", levelGroup.Key.ToString() } }.ToImmutableDictionary();
				foreach (IndentInfo indent in indentInfos.Where(i => !(i.IsValid ?? defaultValidity)))
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, indent.Using.GetLocation(), properties));
				}
			}
		}

		#endregion

		#region Private Types

		public sealed class IndentInfo
		{
			#region Constructors

			public IndentInfo(UsingDirectiveSyntax directive, int level)
			{
				this.Using = directive;
				this.Level = level;

				SyntaxToken keyword = directive.UsingKeyword;
				int keywordLine = keyword.GetLineSpan().StartLinePosition.Line;

				SyntaxTrivia indent = keyword.LeadingTrivia
					.FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)
						&& AtStartOfLine(keywordLine, trivia.GetLineSpan()));
				if (indent.RawKind != 0)
				{
					this.IndentTrivia = indent;
					this.IndentText = indent.ToFullString();

					// This assumes that the user's IndentationSize equals their TabSize, which is almost always true for tab users.
					// Since workspace formatting options aren't available to analyzers (just to code fixers), we have to guess here.
					// This analyzer won't work well for a tab user that uses an indent size different from their tab size.
					int tabCount = this.IndentText.Count(ch => ch == '\t');
					if (this.IndentText.Length == tabCount && tabCount == level)
					{
						this.IsValid = true;
					}
					else if (level == 0)
					{
						this.IsValid = false;
					}

					// Else: There's spaces (and maybe tabs) at level 1+, so we can't validate the indent here.
					// We'll have to wait and see if everything in the group is using the same indent.
				}
				else
				{
					// No indentation is valid if and only if the group of directives is at level 0.
					this.IsValid = level == 0;
				}
			}

			#endregion

			#region Public Properties

			public UsingDirectiveSyntax Using { get; }

			public int Level { get; }

			public string IndentText { get; }

			public bool? IsValid { get; }

			public SyntaxTrivia? IndentTrivia { get; }

			#endregion

			#region Private Methods

			private static bool AtStartOfLine(int line, FileLinePositionSpan span)
			{
				LinePosition position = span.StartLinePosition;
				bool result = position.Line == line && position.Character == 0;
				return result;
			}

			#endregion
		}

		#endregion
	}
}
