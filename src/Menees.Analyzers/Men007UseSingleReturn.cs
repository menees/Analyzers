namespace Menees.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men007UseSingleReturn : Analyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN007";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men007Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men007MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormatVoid =
			new LocalizableResourceString(nameof(Resources.Men007MessageFormatVoid), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men007Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Design, Rules.InfoSeverity, Rules.EnabledByDefault, Description);

		private static readonly DiagnosticDescriptor RuleVoid =
			new(DiagnosticId, Title, MessageFormatVoid, Rules.Design, Rules.InfoSeverity, Rules.EnabledByDefault, Description);

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleVoid);

		#endregion

		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			base.Initialize(context);
			context.RegisterCodeBlockActionHonorExclusions(this, HandleCodeBlock);
		}

		#endregion

		#region Private Methods

		private static bool IsReturnContainer(SyntaxNode node)
		{
			bool result;

			// Checking the node's type (instead of Kind()) allows us to detect lambda expressions
			// and anonymous methods with a single call.  Otherwise, we'd have to search for several
			// different SyntaxKind values, and we might miss some variations.  There are at least:
			// SimpleLambdaExpression, ParenthesizedLambdaExpression, AnonymousMethodExpression
			// C#7 also adds LocalFunctionStatementSyntax, which has a different base type (StatementSyntax).
			// Also, as of VS 2017 Update 15.5, sometimes CodeBlockActions are invoked on whole classes
			// not just functions.  So, we need to check for a few types here instead of dozens of kinds.
			switch (node)
			{
				case AccessorDeclarationSyntax:
				case AnonymousFunctionExpressionSyntax:
				case BaseMethodDeclarationSyntax:
				case LocalFunctionStatementSyntax:
					// Note: If you add another case here, then add another case in GetReturnContainerInfo.
					result = true;
					break;

				default:
					result = false;
					break;
			}

			return result;
		}

		// I originally returned (string name, bool returnsVoid) here, but that caused problems with ValueTuple not being available everywhere.
		private static Tuple<string, bool> GetReturnContainerInfo(SyntaxNode node, IEnumerable<ReturnStatementSyntax> returns)
		{
			string name = null;
			bool returnsVoid = false;

			switch (node)
			{
				case AccessorDeclarationSyntax accessor:
					name = accessor.Keyword + "_";
					switch (accessor.Parent?.Parent)
					{
						case PropertyDeclarationSyntax property:
							name += property.Identifier.Text;
							returnsVoid = accessor.Kind() != SyntaxKind.GetAccessorDeclaration;
							break;

						case EventDeclarationSyntax eventDecl:
							name += eventDecl.Identifier.Text;
							break;

						case IndexerDeclarationSyntax indexer:
							name += "Item";
							break;

						default:
							name += $"<{accessor.Kind()}>";
							break;
					}

					break;

				case AnonymousFunctionExpressionSyntax anon:
					name = $"<{anon.Kind()}>"; // Anonymous functions don't have names.
					returnsVoid = returns.All(ret => ret.Expression == null);
					break;

				case MethodDeclarationSyntax method:
					name = method.Identifier.Text;
					returnsVoid = IsReturnTypeVoid(method.ReturnType);
					break;

				case ConstructorDeclarationSyntax constructor:
					name = constructor.Identifier.Text;
					returnsVoid = true;
					break;

				case DestructorDeclarationSyntax destructor:
					name = destructor.Identifier.Text;
					returnsVoid = true;
					break;

				case BaseMethodDeclarationSyntax baseMethod:
					// This will still apply to ConversionOperatorDeclarationSyntax and OperatorDeclarationSyntax,
					// which are base methods that don't have an explicit Identifier.
					name = $"<{baseMethod.Kind()}>";
					break;

				case LocalFunctionStatementSyntax local:
					name = local.Identifier.Text;
					returnsVoid = IsReturnTypeVoid(local.ReturnType);
					break;
			}

			return Tuple.Create(name, returnsVoid);
		}

		private static bool IsReturnTypeVoid(TypeSyntax type) => type is PredefinedTypeSyntax pre && pre.Keyword.Kind() == SyntaxKind.VoidKeyword;

		private static void HandleCodeBlock(CodeBlockAnalysisContext context)
		{
			// As of VS 2017 Update 15.5, this code block can be an entire class; it's not necessarily a function/accessor.
			SyntaxNode codeBlock = context.CodeBlock;
			IEnumerable<ReturnStatementSyntax> allReturnStatements = codeBlock.DescendantNodesAndSelf()
				.Where(node => node.IsKind(SyntaxKind.ReturnStatement))
				.Cast<ReturnStatementSyntax>()
				.ToList();

			// Group the return statements by the member function/accessor, local function, lambda, or anonymous function that most directly contains them.
			var localBlockGroups = allReturnStatements
				.GroupBy(ret => ret.Ancestors().First(ancestor => ancestor == codeBlock || IsReturnContainer(ancestor)));
			foreach (var localBlockGroup in localBlockGroups)
			{
				SyntaxNode localBlockNode = localBlockGroup.Key;
				IEnumerable<ReturnStatementSyntax> localBlockReturns = localBlockGroup;
				string name;
				bool returnsVoid;
				if (localBlockNode == codeBlock)
				{
					ISymbol symbol = context.OwningSymbol;
					name = symbol.Name;
					returnsVoid = symbol is IMethodSymbol method && method.ReturnsVoid;
				}
				else
				{
					var tuple = GetReturnContainerInfo(localBlockNode, localBlockReturns);
					name = tuple.Item1;
					returnsVoid = tuple.Item2;
				}

				int allowedReturnStatements = returnsVoid ? 0 : 1;
				int count = localBlockReturns.Count();
				if (count > allowedReturnStatements)
				{
					Location blockFirstLineLocation = localBlockNode.GetFirstLineLocation();
					IEnumerable<Location> additionalLocations = localBlockReturns.Select(ret => ret.ReturnKeyword.GetLocation());
					Diagnostic diagnostic;
					if (allowedReturnStatements == 0 && count == 1)
					{
						diagnostic = Diagnostic.Create(RuleVoid, blockFirstLineLocation, additionalLocations, name);
					}
					else
					{
						diagnostic = Diagnostic.Create(Rule, blockFirstLineLocation, additionalLocations, count, name);
					}

					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		#endregion
	}
}
