namespace Menees.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Men015UsePreferredTerms : Analyzer
	{
		#region Public Constants

		public const string DiagnosticId = "MEN015";

		public const string PreferredKey = "Preferred";

		public const string CanFixKey = "CanFix";

		#endregion

		#region Private Data Members

		private static readonly LocalizableString Title =
			new LocalizableResourceString(nameof(Resources.Men015Title), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat =
			new LocalizableResourceString(nameof(Resources.Men015MessageFormat), Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description =
			new LocalizableResourceString(nameof(Resources.Men015Description), Resources.ResourceManager, typeof(Resources));

		private static readonly DiagnosticDescriptor Rule =
			new(DiagnosticId, Title, MessageFormat, Rules.Naming, Rules.InfoSeverity, Rules.EnabledByDefault, Description);

		private static readonly HashSet<SyntaxKind> SimpleIdentifierDeclarationKinds = new()
		{
			SyntaxKind.ClassDeclaration,
			SyntaxKind.StructDeclaration,
			SyntaxKind.InterfaceDeclaration,
			SyntaxKind.RecordDeclaration,
			SyntaxKind.EnumDeclaration,
			SyntaxKind.DelegateDeclaration,
			SyntaxKind.MethodDeclaration,
			SyntaxKind.PropertyDeclaration,
			SyntaxKind.LocalFunctionStatement,
			SyntaxKind.VariableDeclarator,
			SyntaxKind.Parameter,
			SyntaxKind.TypeParameter,
			SyntaxKind.ForEachStatement,
			SyntaxKind.EnumMemberDeclaration,
		};

		#endregion

		#region Public Properties

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		#endregion

		#region Public Methods

		public static string[] SplitIntoTerms(string identifier)
		{
			identifier ??= string.Empty;

			const int ExtraCapacity = 10;
			StringBuilder sb = new(identifier.Length + ExtraCapacity);
			foreach (char ch in identifier)
			{
				if (sb.Length > 0)
				{
					if (char.IsUpper(ch))
					{
						// Don't add multiple spaces and don't separate consecutive capitals.
						char previous = sb[sb.Length - 1];
						if (previous != ' ' && !char.IsUpper(previous))
						{
							sb.Append(' ');
						}
					}
					else if (char.IsDigit(ch))
					{
						// Don't add multiple spaces and don't separate consecutive digits.
						char previous = sb[sb.Length - 1];
						if (previous != ' ' && !char.IsDigit(previous))
						{
							sb.Append(' ');
						}
					}
					else if (char.IsLetter(ch) && sb.Length >= 2 && char.IsUpper(sb[sb.Length - 1]) && char.IsUpper(sb[sb.Length - 2]))
					{
						// There were consecutive capitals followed by a non-captial letter, so insert a space before the last capital.
						// MFCTest --> MFC Test.  CString --> C String.
						sb.Insert(sb.Length - 1, ' ');
					}
				}

				if (ch == '_')
				{
					sb.Append(" _ ");
				}
				else
				{
					sb.Append(ch);
				}
			}

			string spacedTerms = sb.ToString();
			string[] result = spacedTerms.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			return result;
		}

		public override void Initialize(AnalysisContext context)
		{
			base.Initialize(context);
			context.RegisterSyntaxTreeActionHonorExclusions(this, HandleIdentifer);
		}

		#endregion

		#region Private Methods

		private void HandleIdentifer(SyntaxTreeAnalysisContext context)
		{
			if (this.Settings.HasPreferredTerms)
			{
				SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
				if (root != null)
				{
					List<SyntaxToken> identifierTokens = root.DescendantNodesAndTokens()
						.Where(item => item.IsToken && item.IsKind(SyntaxKind.IdentifierToken))
						.Select(item => item.AsToken())
						.Where(token => token.Parent != null && IsIdentifierDeclaration(token.Parent))
						.ToList();

					foreach (SyntaxToken identifier in identifierTokens)
					{
						string text = identifier.Text;
						if (this.Settings.UsePreferredTerm(text, out string preferredTerm))
						{
							Report();
						}
						else
						{
							bool report = false;
							string[] terms = SplitIntoTerms(text);
							List<string> preferredTerms = new(terms.Length);
							foreach (string term in terms)
							{
								if (this.Settings.UsePreferredTerm(term, out preferredTerm))
								{
									preferredTerms.Add(preferredTerm);
									report = true;
								}
								else
								{
									preferredTerms.Add(term);
								}
							}

							if (report)
							{
								preferredTerm = string.Concat(preferredTerms);
								Report();
							}
						}

						void Report()
						{
							// The code fix provider depends on this returning the location of the IdentifierToken.
							Location location = identifier.GetLocation();

							// The code fix provider also needs a custom property to give it the preferred term to use.
							var builder = ImmutableDictionary.CreateBuilder<string, string?>();
							builder.Add(PreferredKey, preferredTerm);

							// The code fix provider can't fix some things (e.g., namespace TestID.Other since only last part can be renamed).
							bool canFix = identifier.Parent != null && SimpleIdentifierDeclarationKinds.Contains(identifier.Parent.Kind());
							builder.Add(CanFixKey, canFix.ToString());

							ImmutableDictionary<string, string?> properties = builder.ToImmutable();
							context.ReportDiagnostic(Diagnostic.Create(Rule, location, properties, preferredTerm, text));
						}
					}
				}
			}
		}

		private bool IsIdentifierDeclaration(SyntaxNode declaration)
		{
			// The SemanticModel.GetDeclaredSymbol(SyntaxNode) extension method says:
			// "This can be any type derived from MemberDeclarationSyntax,
			//     TypeDeclarationSyntax, EnumDeclarationSyntax, NamespaceDeclarationSyntax, ParameterSyntax,
			//     TypeParameterSyntax, or the alias part of a UsingDirectiveSyntax"
			SyntaxKind kind = declaration.Kind();
			bool result = SimpleIdentifierDeclarationKinds.Contains(kind)
				|| (kind == SyntaxKind.IdentifierName
					&& declaration?.Parent?.Kind() == SyntaxKind.QualifiedName
					&& declaration?.Parent?.Parent?.Kind() == SyntaxKind.NamespaceDeclaration)
				|| (kind == SyntaxKind.IdentifierName
					&& declaration?.Parent?.Kind() == SyntaxKind.NameEquals
					&& declaration?.Parent?.Parent?.Kind() == SyntaxKind.UsingDirective);
			return result;
		}

		#endregion
	}
}
