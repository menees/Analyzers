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
	using StyleCop.Analyzers;

	#endregion

	public static class Rules
	{
		#region Public Constants

		public const string Design = nameof(Design);

		public const string Layout = nameof(Layout);

		public const string Naming = nameof(Naming);

		public const string Spacing = nameof(Spacing);

		public const string Usage = nameof(Usage);

		// In debug builds everything will be enabled by default.
		// In release builds we'll disable rules that conflict with StyleCop by default.
		public const bool DisabledByDefault
#if DEBUG
			= true;
#else
			= false;
#endif

		public const bool EnabledByDefault = true;

		// In debug builds we'll treat Info like Warning so VS will show the wavy underlines for it,
		// so we can quickly tell if our diagnostic location information is correct.
		public const DiagnosticSeverity InfoSeverity
#if DEBUG
			= DiagnosticSeverity.Warning;
#else
			= DiagnosticSeverity.Info;
#endif

		#endregion

		#region Public Methods

		public static FileLinePositionSpan GetLineSpan(this SyntaxToken token)
			=> token.SyntaxTree.GetLineSpan(token.Span);

		public static FileLinePositionSpan GetLineSpan(this SyntaxTrivia trivia)
			=> trivia.SyntaxTree.GetLineSpan(trivia.Span);

		public static Tuple<string, Location> GetFileLocation(SyntaxTree tree, SourceText text, int startLine = 0, int endLine = 0)
		{
			Location location = Location.None;
			TextLineCollection lines = text.Lines;
			if (endLine >= startLine && lines.Count > endLine)
			{
				if (endLine == startLine)
				{
					location = Location.Create(tree, lines[startLine].Span);
				}
				else
				{
					TextSpan startSpan = lines[startLine].Span;
					TextSpan endSpan = lines[endLine].Span;
					location = Location.Create(tree, TextSpan.FromBounds(startSpan.Start, endSpan.End));
				}
			}

			string filePath = tree.FilePath;
			string fileName = !string.IsNullOrEmpty(filePath) ? Path.GetFileName(filePath) : string.Empty;
			return Tuple.Create(fileName, location);
		}

		public static Location GetFirstLineLocation(this SyntaxNode node)
		{
			Location result;

			SyntaxTree tree = node.SyntaxTree;
			if (tree.TryGetText(out SourceText text))
			{
				// If the node starts with attribute list(s), then get the start of the first child that's not an attribute list.
				int nodeStart = node.SpanStart;
				ChildSyntaxList children = node.ChildNodesAndTokens();
				if (children[0].IsKind(SyntaxKind.AttributeList))
				{
					// There can be multiple attribute lists on a node, but a node must have at least one leaf token child.
					nodeStart = children.First(child => child.Kind() != SyntaxKind.AttributeList).SpanStart;
				}

				TextSpan lineSpan = text.Lines.GetLineFromPosition(nodeStart).Span;
				TextSpan blockFirstLineSpan = TextSpan.FromBounds(nodeStart, lineSpan.End);
				result = Location.Create(tree, blockFirstLineSpan);
			}
			else
			{
				// If we don't have source text, then "first line" isn't meaningful.
				result = node.GetLocation();
			}

			return result;
		}

		public static void RegisterCodeBlockActionHonorExclusions(
			this AnalysisContext context,
			Action<CodeBlockAnalysisContext> action)
		{
			ConfigureStandardAnalysis(context);

			context.RegisterCodeBlockAction(
				c =>
				{
					SyntaxTree tree = c.CodeBlock.SyntaxTree;
					if (tree != null && !tree.IsGeneratedDocument(c.CancellationToken))
					{
						action(c);
					}
				});
		}

		public static void RegisterSyntaxTreeActionHonorExclusions(
			this AnalysisContext context,
			Action<SyntaxTreeAnalysisContext> action)
		{
			ConfigureStandardAnalysis(context);

			context.RegisterSyntaxTreeAction(
				c =>
				{
					if (!c.IsGeneratedDocument())
					{
						action(c);
					}
				});
		}

		public static void RegisterSyntaxNodeActionHonorExclusions(
			this AnalysisContext context,
			Action<SyntaxNodeAnalysisContext> action,
			params SyntaxKind[] syntaxKinds)
		{
			ConfigureStandardAnalysis(context);

			context.RegisterSyntaxNodeAction(
				c =>
				{
					SyntaxTree tree = c.Node?.SyntaxTree;
					if (tree != null && !tree.IsGeneratedDocument(c.CancellationToken))
					{
						action(c);
					}
				},
				ImmutableArray.Create(syntaxKinds));
		}

		public static bool HasIndicatorAttribute(this SyntaxList<AttributeListSyntax> attributeLists, ISet<string> attributeNames)
		{
			bool result = attributeLists.Any(list => list.Attributes.Any(
				attribute => (attribute.ArgumentList?.Arguments.Count ?? 0) == 0
				&& attribute.Name.IsKind(SyntaxKind.IdentifierName)
				&& attributeNames.Contains(((IdentifierNameSyntax)attribute.Name).Identifier.Text)));
			return result;
		}

		#endregion

		#region Private Methods

		private static void ConfigureStandardAnalysis(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		}

		#endregion
	}
}
