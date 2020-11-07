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
	public sealed class Men008FileNameShouldMatchType : DiagnosticAnalyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN008";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men008Description), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men008MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men008Title), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Rules.Naming, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		private static readonly HashSet<SyntaxKind> SupportedTypeDeclarationKinds = new HashSet<SyntaxKind>
		{
			SyntaxKind.ClassDeclaration,
			SyntaxKind.StructDeclaration,
			SyntaxKind.InterfaceDeclaration,
			SyntaxKind.EnumDeclaration,
			SyntaxKind.DelegateDeclaration,
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

		private static Tuple<string, SyntaxNode> Match(
			string fileNameNoExt,
			IEnumerable<SyntaxNode> typeNodes,
			StringComparison comparison)
		{
			Tuple<string, SyntaxNode> result = null;

			// File names with multiple extensions (e.g., Global.asax.cs) will still have an extension here.
			// Since a C# type name can't contain a '.', we have to do partial name matching for them.
			bool fileNameHadCompoundExt = Path.GetFileNameWithoutExtension(fileNameNoExt) != fileNameNoExt;
			foreach (SyntaxNode typeNode in typeNodes)
			{
				string typeName;
				bool allowPartialMatch = fileNameHadCompoundExt;
				if (typeNode is BaseTypeDeclarationSyntax baseType)
				{
					typeName = baseType.Identifier.ValueText;
					if (!allowPartialMatch)
					{
						// Allow partial types like Xxx to be split into files like Xxx.cs, Xxx.A.cs, etc.
						// Only class, struct, or interface can (legally) be partial types.
						//
						// Also, allow generic types to do partial name matching in case a non-generic type
						// is also present in the same folder (e.g., RowCollection and RowCollection<TRow>).
						allowPartialMatch = baseType.Modifiers.Any(SyntaxKind.PartialKeyword)
							|| (typeNode as TypeDeclarationSyntax)?.TypeParameterList?.Parameters.Count > 0;
					}
				}
				else
				{
					DelegateDeclarationSyntax delegateType = (DelegateDeclarationSyntax)typeNode;
					typeName = delegateType.Identifier.ValueText;
				}

				if (string.Equals(fileNameNoExt, typeName, comparison)
					|| (allowPartialMatch && (fileNameNoExt ?? string.Empty).IndexOf(typeName, comparison) >= 0))
				{
					result = Tuple.Create(typeName, typeNode);
					break;
				}
			}

			return result;
		}

		private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			SyntaxTree tree = context.Tree;
			if (this.settings.IsTypeFileNameCandidate(tree.FilePath))
			{
				SyntaxNode root = tree.GetRoot(context.CancellationToken);
				IEnumerable<SyntaxNode> typeNodes = root.DescendantNodesAndSelf()
					.Where(node => SupportedTypeDeclarationKinds.Contains(node.Kind()));

				// It's possible for a file to contain only comments, an empty namespace, or assembly-level attributes.
				if (typeNodes.Any())
				{
					// Note: This only removes the "last" extension, so Global.asax.cs will produce Global.asax.
					string fileNameNoExt = Path.GetFileNameWithoutExtension(tree.FilePath);
					Tuple<string, SyntaxNode> match = Match(fileNameNoExt, typeNodes, StringComparison.Ordinal);
					if (match == null)
					{
						// We didn't find an exact match, so see if it would match if we use culture rules and ignore case.
						match = Match(fileNameNoExt, typeNodes, StringComparison.CurrentCultureIgnoreCase);
						string fileName = Path.GetFileName(tree.FilePath);
						if (match == null)
						{
							// Use the location of the first type in the file so #pragma or SuppressMessage can be applied to it locally.
							// Otherwise, if I just used the file location, then an assembly-level global suppression would be required.
							Location location = typeNodes.First().GetFirstLineLocation();
							context.ReportDiagnostic(Diagnostic.Create(Rule, location, fileName, "doesn't match the name of a contained type"));
						}
						else
						{
							Location location = match.Item2.GetFirstLineLocation();
							context.ReportDiagnostic(Diagnostic.Create(Rule, location, fileName, $"doesn't exactly match type {match.Item1}"));
						}
					}
				}
			}
		}

		#endregion
	}
}