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
	public sealed class Men006RegionsShouldBeUsed : DiagnosticAnalyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN006";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men006Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men006MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men006Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Rules.Layout, Rules.InfoSeverity, Rules.DisabledByDefault, Description);

		private static readonly HashSet<SyntaxKind> SupportedTypeDeclarationKinds = new HashSet<SyntaxKind>
		{
			SyntaxKind.ClassDeclaration,
			SyntaxKind.StructDeclaration,
			SyntaxKind.InterfaceDeclaration,
			SyntaxKind.EnumDeclaration,

			// Note: We don't care about SyntaxKind.DelegateDeclaration because those will usually be one-liners.
		};

		private static readonly HashSet<string> DesignerGeneratedRegions = new HashSet<string>
		{
			// VS 2002/3 put the designer-generated code in the main file (since partial classes didn't exist until VS 2005).
			// We'll ignore those designer-generated regions.
			"Windows Form Designer generated code",
			"Component Designer generated code",
		};

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

		private static bool ContainsMultipleTypeDeclarations(SyntaxNode root)
		{
			bool result = root.DescendantNodesAndSelf()
				.Where(node => SupportedTypeDeclarationKinds.Contains(node.Kind()))
				.Skip(1)
				.Any();
			return result;
		}

		private static bool HasExistingRegions(SyntaxTreeAnalysisContext context, SyntaxNode root)
		{
			IEnumerable<TextSpan> regionSpans = GetExistingRegionSpans(root);
			bool result = regionSpans.Any();
			if (result)
			{
				// We know something in the text uses a #region, so make sure any types use #regions around all their members.
				IEnumerable<SyntaxNode> typeNodes = root.DescendantNodesAndSelf()
					.Where(node => SupportedTypeDeclarationKinds.Contains(node.Kind()));
				foreach (SyntaxNode typeNode in typeNodes)
				{
					// Use ChildNodes instead of DescendentNodes because we don't want to pick up any class XML comments.
					// Also, look after the open brace to make sure we ignore base class references, class constraints, etc.
					BaseTypeDeclarationSyntax baseType = (BaseTypeDeclarationSyntax)typeNode;
					int openBraceEnd = baseType.OpenBraceToken.Span.End;
					IEnumerable<SyntaxNode> unregionedNodes = typeNode.ChildNodes()
						.Where(node => node.SpanStart > openBraceEnd && !regionSpans.Any(regionSpan => regionSpan.Contains(node.Span)));
					if (unregionedNodes.Any())
					{
						string message = " around all members in " + baseType.Identifier.ValueText;
						Location firstLineLocation = typeNode.GetFirstLineLocation();
						context.ReportDiagnostic(Diagnostic.Create(Rule, firstLineLocation, message));
						continue;
					}
				}

				// Also, make sure that all using directives are in #regions.
				IEnumerable<SyntaxNode> unregionedUsingNodes = root.DescendantNodesAndSelf()
					.Where(node => node.IsKind(SyntaxKind.UsingDirective) && !regionSpans.Any(regionSpan => regionSpan.Contains(node.Span)));
				if (unregionedUsingNodes.Any())
				{
					Location location = unregionedUsingNodes.First().GetFirstLineLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, " around using directives"));
				}
			}

			return result;
		}

		private static IEnumerable<TextSpan> GetExistingRegionSpans(SyntaxNode root)
		{
			IEnumerable<SyntaxNode> regionNodes = root.DescendantNodesAndSelf(descendIntoTrivia: true)
				.Where(node => node.IsKind(SyntaxKind.RegionDirectiveTrivia) || node.IsKind(SyntaxKind.EndRegionDirectiveTrivia));

			// In a perfect world, the #region and #endregion directives will be balanced (even if they're nested).
			// But we have to gracefully handle if they're mismatched or out of order.
			var regionBounds = new List<Tuple<RegionDirectiveTriviaSyntax, EndRegionDirectiveTriviaSyntax>>();
			Stack<RegionDirectiveTriviaSyntax> regionStack = new Stack<RegionDirectiveTriviaSyntax>();
			foreach (SyntaxNode node in regionNodes)
			{
				if (node.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					regionStack.Push((RegionDirectiveTriviaSyntax)node);
				}
				else if (regionStack.Count > 0)
				{
					RegionDirectiveTriviaSyntax regionStart = regionStack.Pop();
					regionBounds.Add(Tuple.Create(regionStart, (EndRegionDirectiveTriviaSyntax)node));
				}
			}

			IEnumerable<TextSpan> result = regionBounds
				.Where(tuple => !DesignerGeneratedRegions.Contains(tuple.Item1.DirectiveNameToken.ValueText ?? string.Empty))
				.Select(tuple => TextSpan.FromBounds(tuple.Item1.FullSpan.Start, tuple.Item2.FullSpan.End));
			return result;
		}

		private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			SyntaxTree tree = context.Tree;
			SyntaxNode root = tree.GetRoot(context.CancellationToken);
			if (root != null && !HasExistingRegions(context, root) && tree.TryGetText(out SourceText text))
			{
				int maxLength = this.settings.MaxUnregionedLines;
				int fileLength = text.Lines.Count;
				if (fileLength > maxLength)
				{
					var fileLocation = Rules.GetFileLocation(tree, text);
					string message = $" because {fileLocation.Item1} is longer than {maxLength} lines (now {fileLength})";
					context.ReportDiagnostic(Diagnostic.Create(Rule, fileLocation.Item2, message));
				}
				else if (ContainsMultipleTypeDeclarations(root))
				{
					var fileLocation = Rules.GetFileLocation(tree, text);
					string message = $" because {fileLocation.Item1} contains multiple type declarations";
					context.ReportDiagnostic(Diagnostic.Create(Rule, fileLocation.Item2, message));
				}
			}
		}

		#endregion
	}
}