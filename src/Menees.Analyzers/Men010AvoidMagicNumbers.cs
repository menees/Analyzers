namespace Menees.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men010AvoidMagicNumbers : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN010";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men010Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men010MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men010Description), Resources.ResourceManager, typeof(Resources));

	private static readonly DiagnosticDescriptor Rule =
		new(DiagnosticId, Title, MessageFormat, Rules.Design, Rules.InfoSeverity, Rules.EnabledByDefault, Description);

	#endregion

	#region Public Properties

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	#endregion

	#region Public Methods

	public override void Initialize(AnalysisContext context)
	{
		base.Initialize(context);
		context.RegisterSyntaxTreeActionHonorExclusions(this, this.HandleSyntaxTree);
	}

	#endregion

	#region Private Methods

	private static bool InAllowedDeclarationContext(LiteralExpressionSyntax literalExpression, Settings settings)
	{
		bool result = literalExpression.Ancestors()
			.Any(ancestor =>
				{
					bool allowed = false;

					switch (ancestor.Kind())
					{
						case SyntaxKind.LocalDeclarationStatement:
							allowed = ((LocalDeclarationStatementSyntax)ancestor).Modifiers.Any(SyntaxKind.ConstKeyword);
							break;

						case SyntaxKind.FieldDeclaration:
							// Allow any static readonly field to use unnamed numeric literals in expressions because sometimes
							// arrays of numeric literals are needed (e.g., Crc32Code) or special instances are created (e.g.,
							// Color.FromArgb(225, 255, 255), new DateTime(1970, 1, 1)).  Also, allow fields that are set to
							// single, non-calculated constants.  In these cases the field name is good enough documentation.
							FieldDeclarationSyntax fieldDeclaration = (FieldDeclarationSyntax)ancestor;
							SyntaxTokenList fieldModifiers = fieldDeclaration.Modifiers;
							allowed = fieldModifiers.Any(SyntaxKind.ConstKeyword)
								|| (fieldModifiers.Any(SyntaxKind.StaticKeyword) && fieldModifiers.Any(SyntaxKind.ReadOnlyKeyword))
								|| HasParentChain(
										literalExpression,
										SyntaxKind.EqualsValueClause,
										SyntaxKind.VariableDeclarator,
										SyntaxKind.VariableDeclaration,
										SyntaxKind.FieldDeclaration);
							break;

						case SyntaxKind.PropertyDeclaration:
							// Allow property initializers that are set to single, non-calculated constants.  The property name is good enough.
							allowed = HasParentChain(literalExpression, SyntaxKind.EqualsValueClause, SyntaxKind.PropertyDeclaration);
							break;

						case SyntaxKind.BracketedArgumentList:
							// Allow small numeric literals as indexer arguments (e.g., for sequential item access).  Since 0, 1,
							// and 2 are allowed by default, it's common to see other small sequential indexes used too.
							// Note: ArrayRankSpecifier (e.g., in new string[7]) is a different syntax kind, so this won't allow
							// magic numbers for array ranks (just indexes).
							allowed = HasParentChain(literalExpression, SyntaxKind.Argument, SyntaxKind.BracketedArgumentList)
								&& byte.TryParse(literalExpression.Token.Text, out byte indexValue);
							break;

						case SyntaxKind.EnumMemberDeclaration:
						case SyntaxKind.AttributeArgument:
							allowed = true;
							break;

						case SyntaxKind.MethodDeclaration:
							// Unit test methods typically use lots of numeric literals for test cases, and they don't need to be named constants.
							allowed = settings.IsUnitTestMethod((BaseMethodDeclarationSyntax)ancestor);
							break;

						case SyntaxKind.ClassDeclaration:
							// Unit test classes typically use lots of numeric literals for test cases, and they don't need to be named constants.
							allowed = settings.IsUnitTestClass((ClassDeclarationSyntax)ancestor);
							break;

						case SyntaxKind.InvocationExpression:
							allowed = IsAllowedInvocation(literalExpression, settings);
							break;

						case SyntaxKind.IndexExpression:
						case SyntaxKind.RangeExpression:
							allowed = literalExpression.Parent == ancestor;
							break;

						case SyntaxKind.SimpleAssignmentExpression:
							allowed = ancestor is AssignmentExpressionSyntax assignment
								&& assignment.Left is MemberAccessExpressionSyntax member
								&& settings.IsAllowedNumericLiteralCaller(member.Name.Identifier.Text);
							break;
					}

					return allowed;
				});

		return result;
	}

	private static bool IsAllowedInvocation(LiteralExpressionSyntax literalExpression, Settings settings)
	{
		bool result = false;

		if (HasParentChain(literalExpression, SyntaxKind.Argument, SyntaxKind.ArgumentList, SyntaxKind.InvocationExpression))
		{
			ArgumentListSyntax? argList = literalExpression?.Parent?.Parent as ArgumentListSyntax;
			if (argList?.Arguments.Count == 1)
			{
				if (argList.Parent is InvocationExpressionSyntax invocation && invocation.Expression != null)
				{
					string? invokedMemberName = null;
					switch (invocation.Expression.Kind())
					{
						case SyntaxKind.IdentifierName:
							// A direct reference to an inherited or declared member with no this or base qualifier.
							IdentifierNameSyntax? identifier = invocation.Expression as IdentifierNameSyntax;
							invokedMemberName = identifier?.Identifier.ValueText;
							break;

						case SyntaxKind.SimpleMemberAccessExpression:
						case SyntaxKind.PointerMemberAccessExpression:
							// A reference like item.Method or item->Method.
							MemberAccessExpressionSyntax? access = invocation.Expression as MemberAccessExpressionSyntax;
							invokedMemberName = access?.Name?.Identifier.ValueText;
							break;
					}

					if (!string.IsNullOrEmpty(invokedMemberName) && invokedMemberName != null && literalExpression != null)
					{
						// Allow cases like item.GetXxx(n) for n in [0,255] to handle cases like IDataRecord.Get and Array.Get accessors.
						if (invokedMemberName.StartsWith("Get"))
						{
							result = Settings.TryParseIntegerLiteral(literalExpression.Token.ValueText, out byte _);
						}
						else
						{
							result = settings.IsAllowedNumericLiteralCaller(invokedMemberName);
						}
					}
				}
			}
		}

		return result;
	}

	private static bool HasParentChain(
		LiteralExpressionSyntax literalExpression,
		SyntaxKind level1,
		SyntaxKind level2)
	{
		bool result = false;

		SyntaxNode? parent = literalExpression.Parent;
		if (parent?.Kind() == level1)
		{
			parent = parent.Parent;
			if (parent?.Kind() == level2)
			{
				result = true;
			}
		}

		return result;
	}

	private static bool HasParentChain(
		LiteralExpressionSyntax literalExpression,
		SyntaxKind level1,
		SyntaxKind level2,
		SyntaxKind level3)
	{
		bool result = false;

		SyntaxNode? parent = literalExpression.Parent;
		if (parent?.Kind() == level1)
		{
			parent = parent.Parent;
			if (parent?.Kind() == level2)
			{
				parent = parent.Parent;
				if (parent?.Kind() == level3)
				{
					result = true;
				}
			}
		}

		return result;
	}

	private static bool HasParentChain(
		LiteralExpressionSyntax literalExpression,
		SyntaxKind level1,
		SyntaxKind level2,
		SyntaxKind level3,
		SyntaxKind level4)
	{
		bool result = false;

		SyntaxNode? parent = literalExpression.Parent;
		if (parent?.Kind() == level1)
		{
			parent = parent.Parent;
			if (parent?.Kind() == level2)
			{
				parent = parent.Parent;
				if (parent?.Kind() == level3)
				{
					parent = parent.Parent;
					if (parent?.Kind() == level4)
					{
						result = true;
					}
				}
			}
		}

		return result;
	}

	private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
	{
		SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);

		// Note: This makes no attempt to push a unary minus into the numeric literal.
		// That's not how C# syntax works, and it would be inconsistent from other unary
		// operators (e.g., + and ~).  Also, if we tried to allow N but not -N, then a user
		// could always get around it by writing an expression like -(N).
		IEnumerable<LiteralExpressionSyntax> magicNumberExpressions = root.DescendantNodesAndSelf()
			.Where(node => node.IsKind(SyntaxKind.NumericLiteralExpression))
			.Cast<LiteralExpressionSyntax>()
			.Where(literal => !this.Settings.IsAllowedNumericLiteral(literal.Token.Text)
				&& !InAllowedDeclarationContext(literal, this.Settings));

		foreach (LiteralExpressionSyntax expression in magicNumberExpressions)
		{
			SyntaxToken literal = expression.Token;
			context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation(), literal.Text));
		}
	}

	#endregion
}