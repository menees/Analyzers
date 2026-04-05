namespace Menees.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men020UsePreferredVarStyle : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN020";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men020Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men020MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men020Description), Resources.ResourceManager, typeof(Resources));

	private static readonly DiagnosticDescriptor Rule =
		new(DiagnosticId, Title, MessageFormat, Rules.Usage, Rules.InfoSeverity, Rules.DisabledByDefault, Description);

	private static readonly string[] FactoryMethodSubstrings =
		["Create", "Build", "Construct", "Make", "Generate", "Produce", "New", "Instance"];

	private static readonly string[] SingletonFieldSubstrings =
		["Empty", "Instance", "Default", "Value"];

	private static readonly HashSet<string> LinqScalarMethodNames = new(StringComparer.Ordinal)
	{
		"First", "FirstOrDefault", "Single", "SingleOrDefault",
		"Last", "LastOrDefault", "ElementAt", "ElementAtOrDefault",
		"MinBy", "MaxBy",
	};

	private static readonly HashSet<string> LinqAggregateMethodNames = new(StringComparer.Ordinal)
	{
		"Count", "LongCount", "Sum", "Average", "Min", "Max",
		"Any", "All", "Contains", "SequenceEqual", "Aggregate",
	};

	#endregion

	#region Public Properties

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	#endregion

	#region Public Methods

	public override void Initialize(AnalysisContext context)
	{
		base.Initialize(context);
		context.RegisterSyntaxNodeActionHonorExclusions(
			this,
			HandleLocalDeclaration,
			SyntaxKind.LocalDeclarationStatement);
		context.RegisterSyntaxNodeActionHonorExclusions(
			this,
			HandleForEachStatement,
			SyntaxKind.ForEachStatement);
	}

	#endregion

	#region Private Methods

	private void HandleLocalDeclaration(SyntaxNodeAnalysisContext context)
	{
		if (this.Settings.HasVarStylePreferences)
		{
			LocalDeclarationStatementSyntax localDecl = (LocalDeclarationStatementSyntax)context.Node;
			VariableDeclarationSyntax declaration = localDecl.Declaration;
			TypeSyntax typeSyntax = declaration.Type;

			// Can't use var with const declarations.
			if (!localDecl.IsConst)
			{
				bool isVar = typeSyntax.IsVar;
				SemanticModel model = context.SemanticModel;

				ITypeSymbol? typeSymbol = model.GetTypeInfo(typeSyntax, context.CancellationToken).Type;

				// Skip unresolvable types and always allow var for anonymous types.
				if (typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error && !typeSymbol.IsAnonymousType)
				{
					bool hasMultipleDeclarators = declaration.Variables.Count > 1;
					VarStyleCategorySettings categorySettings = GetCategorySettings(GetTypeCategory(typeSymbol));
					ExpressionSyntax? initializer = declaration.Variables.Count == 1
						? declaration.Variables[0].Initializer?.Value
						: null;

					AnalyzeVarUsage(context, typeSyntax, isVar, typeSymbol, categorySettings, isForeach: false, initializer, hasMultipleDeclarators, model);
				}
			}
		}
	}

	private void HandleForEachStatement(SyntaxNodeAnalysisContext context)
	{
		if (this.Settings.HasVarStylePreferences)
		{
			ForEachStatementSyntax forEach = (ForEachStatementSyntax)context.Node;
			TypeSyntax typeSyntax = forEach.Type;

			bool isVar = typeSyntax.IsVar;
			SemanticModel model = context.SemanticModel;

			ITypeSymbol? typeSymbol = model.GetTypeInfo(typeSyntax, context.CancellationToken).Type;
			if (typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error && !typeSymbol.IsAnonymousType)
			{
				VarStyleCategorySettings categorySettings = GetCategorySettings(GetTypeCategory(typeSymbol));
				AnalyzeVarUsage(context, typeSyntax, isVar, typeSymbol, categorySettings, isForeach: true, initializer: null, hasMultipleDeclarators: false, model);
			}
		}
	}

	private static void AnalyzeVarUsage(
		SyntaxNodeAnalysisContext context,
		TypeSyntax typeSyntax,
		bool isVar,
		ITypeSymbol typeSymbol,
		VarStyleCategorySettings categorySettings,
		bool isForeach,
		ExpressionSyntax? initializer,
		bool hasMultipleDeclarators,
		SemanticModel model)
	{
		switch (categorySettings.Mode)
		{
			case VarStyleMode.UseExplicitType:
				if (isVar)
				{
					string typeName = typeSymbol.ToMinimalDisplayString(model, typeSyntax.SpanStart);
					context.ReportDiagnostic(Diagnostic.Create(Rule, typeSyntax.GetLocation(), typeName, "var"));
				}

				break;

			case VarStyleMode.UseVar:
				if (categorySettings.HasConditions)
				{
					if (isVar && !IsConditionalVarAllowed(categorySettings, isForeach, initializer, typeSymbol, model))
					{
						string typeName = typeSymbol.ToMinimalDisplayString(model, typeSyntax.SpanStart);
						context.ReportDiagnostic(Diagnostic.Create(Rule, typeSyntax.GetLocation(), typeName, "var"));
					}
				}
				else if (!isVar && !hasMultipleDeclarators)
				{
					if (isForeach || (initializer != null && CanUseVarWithInitializer(initializer, model)))
					{
						context.ReportDiagnostic(Diagnostic.Create(Rule, typeSyntax.GetLocation(), "var", typeSyntax.ToString()));
					}
				}

				break;
		}
	}

	private static bool CanUseVarWithInitializer(ExpressionSyntax initializer, SemanticModel model)
	{
		// var can't be used with null literals, default literals, or implicit object creation (new()).
		bool result = !initializer.IsKind(SyntaxKind.NullLiteralExpression)
			&& !initializer.IsKind(SyntaxKind.DefaultLiteralExpression)
			&& initializer is not ImplicitObjectCreationExpressionSyntax;

		if (result)
		{
			// Check if the expression has a resolvable type (excludes method groups, lambdas, etc.).
			TypeInfo typeInfo = model.GetTypeInfo(initializer);
			result = typeInfo.Type != null && typeInfo.Type.TypeKind != TypeKind.Error;
		}

		return result;
	}

	private static bool IsConditionalVarAllowed(
		VarStyleCategorySettings categorySettings,
		bool isForeach,
		ExpressionSyntax? initializer,
		ITypeSymbol typeSymbol,
		SemanticModel model)
	{
		bool result = categorySettings.Foreach && isForeach;

		if (!result && initializer != null)
		{
			LinqResultCategory linqCategory = GetLinqResultCategory(initializer, model);
			if (linqCategory != LinqResultCategory.None)
			{
				result = linqCategory switch
				{
					LinqResultCategory.Scalar => categorySettings.LinqScalarResult,
					LinqResultCategory.Aggregate => categorySettings.LinqAggregateResult,
					LinqResultCategory.Collection => categorySettings.LinqCollectionResult,
					_ => false,
				};
			}
		}

		if (!result && categorySettings.LongTypeName)
		{
			string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
			result = typeName.Length > categorySettings.LongTypeNameThreshold;
		}

		if (!result && categorySettings.Evident && initializer != null)
		{
			result = IsTypeEvident(initializer, model);
		}

		return result;
	}

	private VarStyleCategorySettings GetCategorySettings(TypeCategory category)
	{
		return category switch
		{
			TypeCategory.BuiltIn => this.Settings.VarBuiltInTypes,
			TypeCategory.Simple => this.Settings.VarSimpleTypes,
			_ => this.Settings.VarElsewhere,
		};
	}

	private static TypeCategory GetTypeCategory(ITypeSymbol type)
	{
		TypeCategory result;

		if (IsBuiltInType(type))
		{
			result = TypeCategory.BuiltIn;
		}
		else if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			result = TypeCategory.Elsewhere;
		}
		else
		{
			result = TypeCategory.Simple;
		}

		return result;
	}

	private static bool IsBuiltInType(ITypeSymbol type)
	{
		bool result;

		if (type.TypeKind == TypeKind.Dynamic)
		{
			result = true;
		}
		else
		{
			result = type.SpecialType switch
			{
				SpecialType.System_Boolean or
				SpecialType.System_Byte or
				SpecialType.System_SByte or
				SpecialType.System_Char or
				SpecialType.System_Decimal or
				SpecialType.System_Double or
				SpecialType.System_Single or
				SpecialType.System_Int32 or
				SpecialType.System_UInt32 or
				SpecialType.System_Int64 or
				SpecialType.System_UInt64 or
				SpecialType.System_Int16 or
				SpecialType.System_UInt16 or
				SpecialType.System_String or
				SpecialType.System_Object or
				SpecialType.System_IntPtr or
				SpecialType.System_UIntPtr => true,
				_ => false,
			};
		}

		return result;
	}

	private static LinqResultCategory GetLinqResultCategory(ExpressionSyntax expression, SemanticModel model)
	{
		LinqResultCategory result = LinqResultCategory.None;

		if (expression is InvocationExpressionSyntax invocation)
		{
			SymbolInfo symbolInfo = model.GetSymbolInfo(invocation);
			if (symbolInfo.Symbol is IMethodSymbol method)
			{
				INamedTypeSymbol? containingType = method.ContainingType;
				if (containingType != null)
				{
					string namespaceName = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
					if (namespaceName == "System.Linq"
						&& (containingType.Name == "Enumerable" || containingType.Name == "Queryable"))
					{
						if (LinqScalarMethodNames.Contains(method.Name))
						{
							result = LinqResultCategory.Scalar;
						}
						else if (LinqAggregateMethodNames.Contains(method.Name))
						{
							result = LinqResultCategory.Aggregate;
						}
						else
						{
							result = LinqResultCategory.Collection;
						}
					}
				}
			}
		}

		return result;
	}

	private static bool IsTypeEvident(ExpressionSyntax expression, SemanticModel model)
	{
		return expression switch
		{
			ObjectCreationExpressionSyntax => true,
			CastExpressionSyntax => true,
			BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AsExpression) => true,
			LiteralExpressionSyntax => true,
			DefaultExpressionSyntax => true,
			TupleExpressionSyntax tuple => tuple.Arguments.All(arg => IsTypeEvident(arg.Expression, model)),
			ArrayCreationExpressionSyntax => true,
			ImplicitArrayCreationExpressionSyntax implicitArray => IsImplicitArrayEvident(implicitArray, model),
			InvocationExpressionSyntax invocation => IsEvidentInvocation(invocation, model),
			MemberAccessExpressionSyntax memberAccess => IsEvidentMemberAccess(memberAccess, model),
			ParenthesizedExpressionSyntax paren => IsTypeEvident(paren.Expression, model),
			_ => false,
		};
	}

	private static bool IsImplicitArrayEvident(ImplicitArrayCreationExpressionSyntax implicitArray, SemanticModel model)
	{
		const int MaxElements = 42;
		InitializerExpressionSyntax? initializer = implicitArray.Initializer;

		bool result = false;
		if (initializer != null)
		{
			SeparatedSyntaxList<ExpressionSyntax> expressions = initializer.Expressions;
			if (expressions.Count > 0 && expressions.Count <= MaxElements)
			{
				result = expressions.All(expr => IsTypeEvident(expr, model));
			}
		}

		return result;
	}

	private static bool IsEvidentInvocation(InvocationExpressionSyntax invocation, SemanticModel model)
	{
		bool result = false;

		SymbolInfo symbolInfo = model.GetSymbolInfo(invocation);
		if (symbolInfo.Symbol is IMethodSymbol method)
		{
			ITypeSymbol returnType = method.ReturnType;
			INamedTypeSymbol? containingType = method.ContainingType;
			if (containingType != null)
			{
				// Non-generic factory method: static method declared in type T, returning T,
				// with method name containing the parent type name or a factory substring.
				if (method.IsStatic
					&& returnType is INamedTypeSymbol nonGenReturn
					&& !nonGenReturn.IsGenericType
					&& SymbolEqualityComparer.Default.Equals(returnType, containingType)
					&& ContainsAnySubstring(method.Name, containingType.Name, FactoryMethodSubstrings))
				{
					result = true;
				}
				// Generic factory method: static method returning generic type,
				// declared in class with same name as return type.
				else if (method.IsStatic
					&& returnType is INamedTypeSymbol genReturn
					&& genReturn.IsGenericType
					&& genReturn.Name == containingType.Name
					&& ContainsAnySubstring(method.Name, containingType.Name, FactoryMethodSubstrings)
					&& AreAllArgumentsEvident(invocation, model))
				{
					result = true;
				}
				// Conversion method: method name is "To" + return type name, and return type is not generic.
				else if (returnType is INamedTypeSymbol convReturn
					&& !convReturn.IsGenericType
					&& method.Name == "To" + convReturn.Name)
				{
					result = true;
				}
				// Generic method with explicit type argument returning the value of that type argument.
				else if (method.TypeArguments.Length > 0
					&& method.TypeArguments.Any(ta => SymbolEqualityComparer.Default.Equals(ta, returnType))
					&& invocation.Expression is MemberAccessExpressionSyntax memberAccess
					&& memberAccess.Name is GenericNameSyntax)
				{
					result = true;
				}
			}
		}

		return result;
	}

	private static bool IsEvidentMemberAccess(MemberAccessExpressionSyntax memberAccess, SemanticModel model)
	{
		bool result = false;

		SymbolInfo symbolInfo = model.GetSymbolInfo(memberAccess);
		if (symbolInfo.Symbol is IFieldSymbol field)
		{
			// Enum member access.
			if (field.ContainingType?.TypeKind == TypeKind.Enum)
			{
				result = true;
			}
			// Singleton field: static/const field returning the type where it's declared,
			// with field name containing the type name or a singleton substring.
			else if ((field.IsStatic || field.IsConst)
				&& SymbolEqualityComparer.Default.Equals(field.Type, field.ContainingType)
				&& ContainsAnySubstring(field.Name, field.ContainingType.Name, SingletonFieldSubstrings))
			{
				result = true;
			}
		}

		return result;
	}

	private static bool AreAllArgumentsEvident(InvocationExpressionSyntax invocation, SemanticModel model)
	{
		ArgumentListSyntax? argumentList = invocation.ArgumentList;

		bool result = false;
		if (argumentList != null)
		{
			result = argumentList.Arguments.All(arg => IsTypeEvident(arg.Expression, model));
		}

		return result;
	}

	private static bool ContainsAnySubstring(string name, string typeName, string[] substrings)
	{
		bool result = name.IndexOf(typeName, StringComparison.Ordinal) >= 0;

		if (!result)
		{
			foreach (string substring in substrings)
			{
				if (name.IndexOf(substring, StringComparison.Ordinal) >= 0)
				{
					result = true;
					break;
				}
			}
		}

		return result;
	}

	#endregion

	#region Private Types

	private enum TypeCategory
	{
		BuiltIn,
		Simple,
		Elsewhere,
	}

	private enum LinqResultCategory
	{
		None,
		Scalar,
		Collection,
		Aggregate,
	}

	#endregion
}
