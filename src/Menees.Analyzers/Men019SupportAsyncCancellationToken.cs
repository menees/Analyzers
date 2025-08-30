namespace Menees.Analyzers;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men019SupportAsyncCancellationToken : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN019";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men019Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men019MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men019Description), Resources.ResourceManager, typeof(Resources));

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

		// The RS1012 rule recommends a registered start action with a private nested type for scope.
		// https://github.com/dotnet/roslyn-analyzers/issues/601
		NestedAnalyzer? nestedAnalyzer = null;
		context.RegisterSymbolActionHonorExclusions(
			this,
			compilation => (nestedAnalyzer = NestedAnalyzer.TryCreate(compilation, this)) != null,
			symbolAnalysisContext => nestedAnalyzer?.HandleMethodSymbol(symbolAnalysisContext),
			SymbolKind.Method);
	}

	#endregion

	#region Private Types

	private sealed class NestedAnalyzer(
		Analyzer caller,
		INamedTypeSymbol cancellationTokenType,
		INamedTypeSymbol[] fixedTaskTypes,
		INamedTypeSymbol[] genericTaskTypes)
	{
		#region Private Data Members

		private readonly Analyzer callingAnalyzer = caller;
		private readonly INamedTypeSymbol cancellationTokenType = cancellationTokenType;
		private readonly INamedTypeSymbol[] fixedTaskTypes = fixedTaskTypes;
		private readonly INamedTypeSymbol[] genericTaskTypes = genericTaskTypes;

		#endregion

		#region Public Methods

		public static NestedAnalyzer? TryCreate(Compilation compilation, Analyzer caller)
		{
			INamedTypeSymbol? cancellationTokenType = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName);
			INamedTypeSymbol? taskType = compilation.GetTypeByMetadataName(typeof(Task).FullName);
			INamedTypeSymbol? task1Type = compilation.GetTypeByMetadataName(typeof(Task<>).FullName);
			INamedTypeSymbol? valueTaskType = compilation.GetTypeByMetadataName(typeof(ValueTask).FullName);
			INamedTypeSymbol? valueTask1Type = compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName);

			NestedAnalyzer? result = cancellationTokenType != null && taskType != null && task1Type != null && valueTaskType != null && valueTask1Type != null
				? new(caller, cancellationTokenType, [taskType, valueTaskType], [task1Type, valueTask1Type])
				: null;
			return result;
		}

		public void HandleMethodSymbol(SymbolAnalysisContext context)
		{
			IMethodSymbol method = (IMethodSymbol)context.Symbol;
			Settings settings = this.callingAnalyzer.Settings;

			if ((method.DeclaredAccessibility > Accessibility.Private || settings.CheckPrivateMethodsForCancellation)
				&& !method.IsImplicitlyDeclared
				&& IsAsyncMethodKindSupported(method.MethodKind)
				&& !method.ReturnsVoid
				&& method.ReturnType is not null  // Roslyn's ISymbolExtensions.IsAwaitableNonDynamic checks this
				&& (method.IsAsync || IsAwaitable(method.ReturnType))
				&& !method.IsOverride
				&& method.ExplicitInterfaceImplementations.IsDefaultOrEmpty
				&& !IsImplicitInterfaceImplementation(method)
				&& !settings.IsUnitTestMethod(method))
			{
				List<IParameterSymbol> parameters = GetEligibleParameters(method);
				if (!HasCancellationTokenParameter(parameters)
					&& !parameters.Any(ParameterHasCancellationTokenProperty))
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.Name));
				}
			}
		}

		#endregion

		#region Private Methods

		private static bool IsAsyncMethodKindSupported(MethodKind methodKind)
		{
			bool result = methodKind switch
			{
				// These can't be async
				MethodKind.Constructor or
				MethodKind.Conversion or
				MethodKind.UserDefinedOperator or
				MethodKind.PropertyGet or
				MethodKind.PropertySet or
				MethodKind.StaticConstructor or
				MethodKind.BuiltinOperator or
				MethodKind.Destructor or
				MethodKind.EventAdd or
				MethodKind.EventRaise or
				MethodKind.EventRemove or
				MethodKind.DelegateInvoke or
				MethodKind.FunctionPointerSignature => false,

				// We're ignoring interface implementations
				MethodKind.ExplicitInterfaceImplementation => false,

				// AnonymousFunction, Ordinary, ReducedExtension, DeclareMethod, LocalFunction
				_ => true,
			};

			return result;
		}

		private static List<IParameterSymbol> GetEligibleParameters(IMethodSymbol method)
			=> [.. method.Parameters.Where(p => p.RefKind != RefKind.Out)];

		private static bool TryFindFirstMethod(
			ITypeSymbol type,
			string methodName,
			Predicate<IMethodSymbol> match,
			[NotNullWhen(true)] out IMethodSymbol? method)
		{
			method = type.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault(m => match(m));
			return method != null;
		}

		private bool IsAwaitable(ITypeSymbol type)
		{
			bool result = false;

			if (this.fixedTaskTypes.Contains(type, SymbolEqualityComparer.IncludeNullability)
				|| (type is INamedTypeSymbol { IsGenericType: true } namedType
					&& this.genericTaskTypes.Contains(namedType.OriginalDefinition, SymbolEqualityComparer.IncludeNullability)))
			{
				result = true;
			}
			else
			{
				// From 2018 at https://github.com/DotNetAnalyzers/AsyncUsageAnalyzers/pull/70#discussion_r209184372
				// For a more modern and complex version that also handles extension methods, see:
				// https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Extensions/Symbols/ISymbolExtensions.cs#L640
				result = TryFindFirstMethod(type, "GetAwaiter", x => x.Parameters.Length == 0, out IMethodSymbol? method)
					&& method.ReturnType is ITypeSymbol returnType
					&& TryFindFirstMethod(returnType, "GetResult", x => x.Parameters.Length == 0, out _);
			}

			return result;
		}

		private bool IsImplicitInterfaceImplementation(IMethodSymbol method)
		{
			bool result = false;

			if (method.DeclaredAccessibility == Accessibility.Public)
			{
				INamedTypeSymbol containingType = method.ContainingType;
				if (containingType.TypeKind is TypeKind.Class or TypeKind.Struct)
				{
					foreach (ISymbol member in containingType.AllInterfaces.SelectMany(intf => intf.GetMembers(method.Name)))
					{
						ISymbol? interfaceImplementation = containingType.FindImplementationForInterfaceMember(member);
						if (method.Equals(interfaceImplementation, SymbolEqualityComparer.IncludeNullability))
						{
							result = true;
							break;
						}
					}
				}
			}

			return result;
		}

		private bool HasCancellationTokenParameter(List<IParameterSymbol> parameters)
		{
			bool result = false;

			foreach (IParameterSymbol parameterSymbol in parameters)
			{
				INamedTypeSymbol? parameterType = parameterSymbol.Type as INamedTypeSymbol;
				if (this.cancellationTokenType.Equals(parameterType, SymbolEqualityComparer.IncludeNullability))
				{
					result = true;
					break;
				}
			}

			return result;
		}

		private bool ParameterHasCancellationTokenProperty(IParameterSymbol parameterSymbol)
		{
			bool result = false;

			ISet<string> propertyNamesToCheck = this.callingAnalyzer.Settings.PropertyNamesForCancellation;
			if (propertyNamesToCheck.Count > 0)
			{
				IEnumerable<IPropertySymbol> publicCancellationProperties = propertyNamesToCheck
					.SelectMany(propertyName => parameterSymbol.Type.GetMembers(propertyName))
					.Where(m => m.Kind == SymbolKind.Property && m.DeclaredAccessibility == Accessibility.Public)
					.Cast<IPropertySymbol>();
				foreach (IPropertySymbol propertySymbol in publicCancellationProperties)
				{
					if (this.cancellationTokenType.Equals(propertySymbol.Type, SymbolEqualityComparer.IncludeNullability))
					{
						result = true;
						break;
					}
				}
			}

			return result;
		}

		#endregion
	}

	#endregion
}
