namespace Menees.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men003MethodTooLong : Analyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN003";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men003Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men003MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men003Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Layout, DiagnosticSeverity.Warning, Rules.EnabledByDefault, Description);

		private static readonly HashSet<SyntaxKind> SupportedSyntaxKinds =
		[
			SyntaxKind.MethodDeclaration,
			SyntaxKind.ConstructorDeclaration,
			SyntaxKind.DestructorDeclaration,
			SyntaxKind.ConversionOperatorDeclaration,
			SyntaxKind.OperatorDeclaration,
		];

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		#endregion

		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			base.Initialize(context);
			context.RegisterCodeBlockActionHonorExclusions(this, this.HandleMethod);
		}

		#endregion

		#region Internal Methods

		internal static void HandleBlockTooLong(
			CodeBlockAnalysisContext context,
			ISet<SyntaxKind> supportedSyntaxKinds,
			DiagnosticDescriptor descriptor,
			int maxLineCount)
		{
			if (context.OwningSymbol.Kind == SymbolKind.Method)
			{
				SyntaxNode block = context.CodeBlock;
				SyntaxKind blockKind = block.Kind();
				if (supportedSyntaxKinds.Contains(blockKind))
				{
					SyntaxTree tree = block.SyntaxTree;
					if (tree != null && tree.TryGetText(out SourceText? treeText))
					{
						SourceText blockText = treeText.GetSubText(block.Span);
						int blockLineCount = blockText.Lines.Count;
						if (blockLineCount > maxLineCount)
						{
							Location location = block.GetFirstLineLocation();
							string blockName = context.OwningSymbol.Name;
							string blockDescription = GetBlockDescription(blockName, blockKind, context.OwningSymbol?.ContainingType?.Name);
							context.ReportDiagnostic(Diagnostic.Create(descriptor, location, blockDescription, maxLineCount, blockLineCount));
						}
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private static string GetBlockDescription(string blockName, SyntaxKind blockKind, string? containingTypeName)
		{
			string result;
			switch (blockKind)
			{
				case SyntaxKind.ConstructorDeclaration:
					result = (blockName == ".cctor" ? "Static constructor " : "Constructor ") + (containingTypeName ?? blockName);
					break;

				case SyntaxKind.DestructorDeclaration:
					result = "Destructor " + ('~' + containingTypeName ?? blockName);
					break;

				case SyntaxKind.ConversionOperatorDeclaration:
				case SyntaxKind.OperatorDeclaration:
					// Improve: This could describe Implicit and Explicit conversion operators better (e.g., show converted type name).
					result = "Operator " + TrimPrefix(blockName, "op_");
					break;

				case SyntaxKind.GetAccessorDeclaration:
					// Improve: This could describe indexers better (e.g., report this instead of Item).
					result = "Property " + TrimPrefix(blockName, "get_") + " get accessor";
					break;

				case SyntaxKind.SetAccessorDeclaration:
					// Improve: This could describe indexers better (e.g., report this instead of Item).
					result = "Property " + TrimPrefix(blockName, "set_") + " set accessor";
					break;

				case SyntaxKind.AddAccessorDeclaration:
					result = "Event " + TrimPrefix(blockName, "add_") + " add accessor";
					break;

				case SyntaxKind.RemoveAccessorDeclaration:
					result = "Event " + TrimPrefix(blockName, "remove_") + " remove accessor";
					break;

				default:
					result = TrimSuffix(blockKind.ToString(), "Declaration") + ' ' + blockName;
					break;
			}

			return result;
		}

		private static string TrimPrefix(string name, string prefix)
		{
			string result = name.StartsWith(prefix) ? name.Substring(prefix.Length) : name;
			return result;
		}

		private static string TrimSuffix(string name, string suffix)
		{
			string result = name.EndsWith(suffix) ? name.Substring(0, name.Length - suffix.Length) : name;
			return result;
		}

		private void HandleMethod(CodeBlockAnalysisContext context)
		{
			HandleBlockTooLong(context, SupportedSyntaxKinds, Rule, this.Settings.MaxMethodLines);
		}

		#endregion
	}
}
