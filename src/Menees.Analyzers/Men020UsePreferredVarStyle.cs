namespace Menees.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men020UsePreferredVarStyle : Analyzer
{
	#region Public Constants

	public const string DiagnosticIdBuiltIn = "MEN020B";

	public const string DiagnosticIdSimple = "MEN020S";

	public const string DiagnosticIdElsewhere = "MEN020E";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men020MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly DiagnosticDescriptor RuleBuiltIn = new(
		DiagnosticIdBuiltIn,
		new LocalizableResourceString(nameof(Resources.Men020BTitle), Resources.ResourceManager, typeof(Resources)),
		MessageFormat,
		Rules.Usage,
		Rules.InfoSeverity,
		Rules.DisabledByDefault,
		new LocalizableResourceString(nameof(Resources.Men020BDescription), Resources.ResourceManager, typeof(Resources)));

	private static readonly DiagnosticDescriptor RuleSimple = new(
		DiagnosticIdSimple,
		new LocalizableResourceString(nameof(Resources.Men020STitle), Resources.ResourceManager, typeof(Resources)),
		MessageFormat,
		Rules.Usage,
		Rules.InfoSeverity,
		Rules.DisabledByDefault,
		new LocalizableResourceString(nameof(Resources.Men020SDescription), Resources.ResourceManager, typeof(Resources)));

	private static readonly DiagnosticDescriptor RuleElsewhere = new(
		DiagnosticIdElsewhere,
		new LocalizableResourceString(nameof(Resources.Men020ETitle), Resources.ResourceManager, typeof(Resources)),
		MessageFormat,
		Rules.Usage,
		Rules.InfoSeverity,
		Rules.DisabledByDefault,
		new LocalizableResourceString(nameof(Resources.Men020EDescription), Resources.ResourceManager, typeof(Resources)));

	private static readonly HashSet<string> LinqScalarMethodNames = new(StringComparer.Ordinal)
	{
		"First", "FirstOrDefault", "Single", "SingleOrDefault",
		"Last", "LastOrDefault", "ElementAt", "ElementAtOrDefault",
	};

	private static readonly HashSet<string> LinqAggregateMethodNames = new(StringComparer.Ordinal)
	{
		"Count", "LongCount", "Sum", "Average", "Min", "MinBy", "Max", "MaxBy",
		"Any", "All", "Contains", "SequenceEqual", "Aggregate", "AggregateBy",
		"TryGetNonEnumeratedCount",
	};

	private static readonly SymbolDisplayFormat NullableAwareMinimalFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
		.WithMiscellaneousOptions(
			SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions
			| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	#endregion

	#region Public Properties

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleBuiltIn, RuleSimple, RuleElsewhere);

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
		context.RegisterSyntaxNodeActionHonorExclusions(
			this,
			HandleForStatement,
			SyntaxKind.ForStatement);
		context.RegisterSyntaxNodeActionHonorExclusions(
			this,
			HandleUsingStatement,
			SyntaxKind.UsingStatement);
		context.RegisterSyntaxNodeActionHonorExclusions(
			this,
			HandleDeclarationExpression,
			SyntaxKind.DeclarationExpression);
	}

	#endregion

	#region Private Methods

	private void HandleLocalDeclaration(SyntaxNodeAnalysisContext context)
	{
		if (this.Settings.HasVarStylePreferences)
		{
			LocalDeclarationStatementSyntax localDecl = (LocalDeclarationStatementSyntax)context.Node;

			// Can't use var with const declarations.
			if (!localDecl.IsConst)
			{
				AnalyzeVariableDeclaration(context, localDecl.Declaration);
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

			TypeInfo typeInfo = model.GetTypeInfo(typeSyntax, context.CancellationToken);
			ITypeSymbol? typeSymbol = GetNullableAdjustedType(typeInfo, initializer: null, model);
			if (typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error && !typeSymbol.IsAnonymousType)
			{
				TypeCategory category = GetTypeCategory(typeSymbol);
				VarStyleCategorySettings categorySettings = GetCategorySettings(category);
				AnalyzeVarUsage(context, typeSyntax, isVar, typeSymbol, category, categorySettings, isForeach: true, initializer: null, hasMultipleDeclarators: false, isDeclarationExpression: false, model);
			}
		}
	}

	private void HandleForStatement(SyntaxNodeAnalysisContext context)
	{
		if (this.Settings.HasVarStylePreferences)
		{
			ForStatementSyntax forStatement = (ForStatementSyntax)context.Node;
			if (forStatement.Declaration != null)
			{
				AnalyzeVariableDeclaration(context, forStatement.Declaration);
			}
		}
	}

	private void HandleUsingStatement(SyntaxNodeAnalysisContext context)
	{
		if (this.Settings.HasVarStylePreferences)
		{
			UsingStatementSyntax usingStatement = (UsingStatementSyntax)context.Node;
			if (usingStatement.Declaration != null)
			{
				AnalyzeVariableDeclaration(context, usingStatement.Declaration);
			}
		}
	}

	private void HandleDeclarationExpression(SyntaxNodeAnalysisContext context)
	{
		if (this.Settings.HasVarStylePreferences)
		{
			DeclarationExpressionSyntax declExpr = (DeclarationExpressionSyntax)context.Node;

			// Only handle single variable designations (out var, individual deconstruction elements).
			// Skip parenthesized designations (var (x, y) = ...) as they require different syntax transforms.
			if (declExpr.Designation is SingleVariableDesignationSyntax)
			{
				TypeSyntax typeSyntax = declExpr.Type;
				bool isVar = typeSyntax.IsVar;
				SemanticModel model = context.SemanticModel;

				TypeInfo typeInfo = model.GetTypeInfo(typeSyntax, context.CancellationToken);
				ITypeSymbol? typeSymbol = GetNullableAdjustedType(typeInfo, initializer: null, model);
				if (typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error && !typeSymbol.IsAnonymousType)
				{
					TypeCategory category = GetTypeCategory(typeSymbol);
					VarStyleCategorySettings categorySettings = GetCategorySettings(category);
					AnalyzeVarUsage(context, typeSyntax, isVar, typeSymbol, category, categorySettings,
						isForeach: false, initializer: null, hasMultipleDeclarators: false, isDeclarationExpression: true, model);
				}
			}
		}
	}

	private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context, VariableDeclarationSyntax declaration)
	{
		TypeSyntax typeSyntax = declaration.Type;
		bool isVar = typeSyntax.IsVar;
		SemanticModel model = context.SemanticModel;

		ExpressionSyntax? initializer = declaration.Variables.Count == 1
			? declaration.Variables[0].Initializer?.Value
			: null;

		TypeInfo typeInfo = model.GetTypeInfo(typeSyntax, context.CancellationToken);
		ITypeSymbol? typeSymbol = GetNullableAdjustedType(typeInfo, initializer, model);

		// Skip unresolvable types and always allow var for anonymous types.
		if (typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error && !typeSymbol.IsAnonymousType)
		{
			bool hasMultipleDeclarators = declaration.Variables.Count > 1;
			TypeCategory category = GetTypeCategory(typeSymbol);
			VarStyleCategorySettings categorySettings = GetCategorySettings(category);

			AnalyzeVarUsage(context, typeSyntax, isVar, typeSymbol, category, categorySettings, isForeach: false, initializer, hasMultipleDeclarators, isDeclarationExpression: false, model);
		}
	}

	private static void AnalyzeVarUsage(
		SyntaxNodeAnalysisContext context,
		TypeSyntax typeSyntax,
		bool isVar,
		ITypeSymbol typeSymbol,
		TypeCategory category,
		VarStyleCategorySettings categorySettings,
		bool isForeach,
		ExpressionSyntax? initializer,
		bool hasMultipleDeclarators,
		bool isDeclarationExpression,
		SemanticModel model)
	{
		DiagnosticDescriptor rule = GetRule(category);

		switch (categorySettings.Mode)
		{
			case VarStyleMode.UseExplicitType:
				if (isVar)
				{
					string typeName = typeSymbol.ToMinimalDisplayString(model, typeSyntax.SpanStart, NullableAwareMinimalFormat);
					context.ReportDiagnostic(Diagnostic.Create(rule, typeSyntax.GetLocation(), typeName, "var"));
				}

				break;

			case VarStyleMode.UseVar:
				if (categorySettings.HasConditions)
				{
					if (isVar && !IsConditionalVarAllowed(categorySettings, isForeach, initializer, typeSymbol, model))
					{
						string typeName = typeSymbol.ToMinimalDisplayString(model, typeSyntax.SpanStart, NullableAwareMinimalFormat);
						context.ReportDiagnostic(Diagnostic.Create(rule, typeSyntax.GetLocation(), typeName, "var"));
					}
				}
				else if (!isVar && !hasMultipleDeclarators)
				{
					if (isForeach || isDeclarationExpression || (initializer != null && CanUseVarWithInitializer(initializer, model)))
					{
						context.ReportDiagnostic(Diagnostic.Create(rule, typeSyntax.GetLocation(), "var", typeSyntax.ToString()));
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
			string typeName = typeSymbol.ToDisplayString(NullableAwareMinimalFormat);
			result = typeName.Length >= categorySettings.LongTypeNameLength;
		}

		if (!result && categorySettings.Evident && initializer != null)
		{
			result = IsTypeEvident(initializer, model);
		}

		return result;
	}

	private static ITypeSymbol? GetNullableAdjustedType(TypeInfo typeInfo, ExpressionSyntax? initializer, SemanticModel model)
	{
		ITypeSymbol? type = typeInfo.Type;

		// When using var in a nullable context, Roslyn may report the type as Annotated (nullable).
		// We need to determine whether the value is actually non-null so we can use the non-nullable type.
		if (type != null
			&& type.IsReferenceType
			&& type.NullableAnnotation == NullableAnnotation.Annotated)
		{
			// Check the var keyword's flow state first (works in full compilation).
			if (typeInfo.Nullability.FlowState == NullableFlowState.NotNull)
			{
				type = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
			}
			// Fall back to the initializer's type annotation (handles adhoc workspaces
			// where flow analysis may not be fully available).
			else if (initializer != null)
			{
				ITypeSymbol? initType = model.GetTypeInfo(initializer).Type;
				if (initType != null && initType.NullableAnnotation != NullableAnnotation.Annotated)
				{
					type = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
				}
			}
		}

		return type;
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

	private static DiagnosticDescriptor GetRule(TypeCategory category)
	{
		return category switch
		{
			TypeCategory.BuiltIn => RuleBuiltIn,
			TypeCategory.Simple => RuleSimple,
			_ => RuleElsewhere,
		};
	}

	private static TypeCategory GetTypeCategory(ITypeSymbol type)
	{
		TypeCategory result;

		if (IsBuiltInType(type))
		{
			result = TypeCategory.BuiltIn;
		}
		else
		{
			// Unwrap array types to categorize based on their element type.
			// For example, KeyValuePair<string, int>[] should be Elsewhere (generic element type).
			ITypeSymbol effectiveType = type;
			while (effectiveType is IArrayTypeSymbol arrayType)
			{
				effectiveType = arrayType.ElementType;
			}

			if (effectiveType is INamedTypeSymbol namedType && namedType.IsGenericType)
			{
				// Nullable value types (e.g., int?) use simple '?' suffix syntax,
				// so treat them as Simple rather than Elsewhere.
				result = namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
					? TypeCategory.Simple
					: TypeCategory.Elsewhere;
			}
			else
			{
				result = TypeCategory.Simple;
			}
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
			AwaitExpressionSyntax awaitExpr => IsEvidentAwaitExpression(awaitExpr, model),
			MemberAccessExpressionSyntax memberAccess => IsEvidentMemberAccess(memberAccess, model),
			ConditionalExpressionSyntax conditional =>
				IsTypeEvident(conditional.WhenTrue, model) && IsTypeEvident(conditional.WhenFalse, model),
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
		SymbolInfo symbolInfo = model.GetSymbolInfo(invocation);
		return symbolInfo.Symbol is IMethodSymbol method
			&& IsEvidentMethodCall(method, method.ReturnType, invocation, model);
	}

	private static bool IsEvidentAwaitExpression(AwaitExpressionSyntax awaitExpr, SemanticModel model)
	{
		bool result = false;

		if (awaitExpr.Expression is InvocationExpressionSyntax invocation)
		{
			SymbolInfo symbolInfo = model.GetSymbolInfo(invocation);
			if (symbolInfo.Symbol is IMethodSymbol method)
			{
				// Check if the method's actual return type (e.g., Task<T>) is evident.
				result = IsEvidentMethodCall(method, method.ReturnType, invocation, model);

				// Check if the awaited type (e.g., T from Task<T>) makes the method evident.
				if (!result)
				{
					ITypeSymbol? awaitedType = model.GetTypeInfo(awaitExpr).Type;
					if (awaitedType != null)
					{
						result = IsEvidentMethodCall(method, awaitedType, invocation, model);
					}
				}
			}
		}

		return result;
	}

	private static bool IsEvidentMethodCall(
		IMethodSymbol method,
		ITypeSymbol returnType,
		InvocationExpressionSyntax invocation,
		SemanticModel model)
	{
		bool result = false;

		INamedTypeSymbol? containingType = method.ContainingType;
		if (containingType != null)
		{
			// Non-generic factory method: static method declared in type T returning T.
			// The type name is visible at the call site (e.g., Guid.NewGuid(), Task.Run()).
			if (method.IsStatic
				&& returnType is INamedTypeSymbol nonGenReturn
				&& !nonGenReturn.IsGenericType
				&& SymbolEqualityComparer.Default.Equals(returnType, containingType))
			{
				result = true;
			}
			// Generic factory method: static method returning generic type T<...>,
			// declared in class T, with all arguments evident (so type args are determinable).
			else if (method.IsStatic
				&& returnType is INamedTypeSymbol genReturn
				&& genReturn.IsGenericType
				&& genReturn.Name == containingType.Name
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
			// Fluent/builder instance method: returns the containing type (e.g., StringBuilder.Append).
			// Only evident when the receiver expression is itself type-evident, so the type name
			// is visible somewhere in the call chain (e.g., new StringBuilder().Append("x")).
			else if (!method.IsStatic
				&& SymbolEqualityComparer.Default.Equals(returnType, containingType)
				&& invocation.Expression is MemberAccessExpressionSyntax fluentAccess
				&& IsTypeEvident(fluentAccess.Expression, model))
			{
				result = true;
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
			// Static/const field returning the type where it's declared (e.g., IPAddress.Any,
			// string.Empty, Color.Red). The containing type name is visible in the member access.
			else if ((field.IsStatic || field.IsConst)
				&& SymbolEqualityComparer.Default.Equals(field.Type, field.ContainingType))
			{
				result = true;
			}
		}
		else if (symbolInfo.Symbol is IPropertySymbol property)
		{
			// Static property returning the type where it's declared (e.g., Encoding.UTF8,
			// CultureInfo.InvariantCulture, TimeProvider.System).
			if (property.IsStatic
				&& SymbolEqualityComparer.Default.Equals(property.Type, property.ContainingType))
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
