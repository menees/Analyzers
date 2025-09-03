namespace Menees.Analyzers;

using System;
using System.Collections.Concurrent;
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

	private static readonly SymbolEqualityComparer SymbolComparer = SymbolEqualityComparer.IncludeNullability;

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
			symbolAnalysisContext => nestedAnalyzer?.HandleNamedTypeSymbol(symbolAnalysisContext),
			SymbolKind.NamedType);
	}

	#endregion

	#region Private Types

	private sealed class NestedAnalyzer(
		Analyzer caller,
		INamedTypeSymbol cancellationTokenType,
		INamedTypeSymbol[] fixedTaskTypes,
		INamedTypeSymbol[] genericTaskTypes,
		INamedTypeSymbol asyncMethodBuilderAttributeType)
	{
		#region Private Data Members

		private readonly Analyzer callingAnalyzer = caller;
		private readonly INamedTypeSymbol cancellationTokenType = cancellationTokenType;
		private readonly INamedTypeSymbol[] fixedTaskTypes = fixedTaskTypes;
		private readonly INamedTypeSymbol[] genericTaskTypes = genericTaskTypes;
		private readonly INamedTypeSymbol asyncMethodBuilderAttributeType = asyncMethodBuilderAttributeType;
#pragma warning disable IDE0079 // Remove unnecessary suppression. False positive.
#pragma warning disable RS1024 // Compare symbols correctly. False positive; this is using a SymbolEqualityComparer.
		private readonly ConcurrentDictionary<ITypeSymbol, bool> typeHasCancellationTokenProperty = new(SymbolComparer);
#pragma warning restore RS1024 // Compare symbols correctly
#pragma warning restore IDE0079 // Remove unnecessary suppression

		#endregion

		#region Public Methods

		public static NestedAnalyzer? TryCreate(Compilation compilation, Analyzer caller)
		{
			INamedTypeSymbol? cancellationTokenType = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName);
			INamedTypeSymbol? taskType = compilation.GetTypeByMetadataName(typeof(Task).FullName);
			INamedTypeSymbol? task1Type = compilation.GetTypeByMetadataName(typeof(Task<>).FullName);
			INamedTypeSymbol? valueTaskType = compilation.GetTypeByMetadataName(typeof(ValueTask).FullName);
			INamedTypeSymbol? valueTask1Type = compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName);
			INamedTypeSymbol? asyncMethodBuilderAttributeType = compilation.GetTypeByMetadataName(typeof(AsyncMethodBuilderAttribute).FullName);

			NestedAnalyzer? result = cancellationTokenType != null && taskType != null && task1Type != null
					&& valueTaskType != null && valueTask1Type != null && asyncMethodBuilderAttributeType != null
				? new(caller, cancellationTokenType, [taskType, valueTaskType], [task1Type, valueTask1Type], asyncMethodBuilderAttributeType)
				: null;
			return result;
		}

		public void HandleNamedTypeSymbol(SymbolAnalysisContext context)
		{
			// Look at each overload method group (including inherited methods).
			// If a method group has any eligible async/awaitable methods and none
			// of those are cancellable, then we'll report each eligible method.
			INamedTypeSymbol namedType = (INamedTypeSymbol)context.Symbol;

#pragma warning disable IDE0079 // Remove unnecessary suppression. False positive!
#pragma warning disable RS1024 // Compare symbols correctly. False positive! We're grouping by the Name not IMethodSymbol.
			IEnumerable<IGrouping<string, IMethodSymbol>> methodGroups = GetTypeAndBaseTypes(namedType)
				.SelectMany(type => type.GetMembers())
				.OfType<IMethodSymbol>()
				.GroupBy(m => m.Name, m => m, StringComparer.Ordinal);
#pragma warning restore RS1024 // Compare symbols correctly
#pragma warning restore IDE0079 // Remove unnecessary suppression

			Settings settings = this.callingAnalyzer.Settings;
			foreach (IGrouping<string, IMethodSymbol> methodGroup in methodGroups)
			{
				List<IMethodSymbol> eligibleMethods = [];
				bool isAtLeastOneOverloadCancellable = false;
				foreach (IMethodSymbol method in methodGroup)
				{
					if ((method.DeclaredAccessibility > Accessibility.Private || settings.CheckPrivateMethodsForCancellation)
						&& !method.IsImplicitlyDeclared
						&& IsAsyncMethodKindSupported(method.MethodKind)
						&& !method.ReturnsVoid
						&& method.ReturnType is not null  // Roslyn's ISymbolExtensions.IsAwaitableNonDynamic checks this
						&& (method.IsAsync || IsAwaitable(method.ReturnType))
						&& !method.IsOverride
						&& method.ExplicitInterfaceImplementations.IsDefaultOrEmpty
						&& !IsImplicitInterfaceImplementation(method)
						&& !settings.IsUnitTestMethod(method)
						&& !IsAssemblyEntryPoint(method, ref context))
					{
						eligibleMethods.Add(method);

						List<IParameterSymbol> parameters = GetEligibleParameters(method);
						if (HasCancellationTokenParameter(parameters)
							|| parameters.Any(ParameterHasCancellationTokenProperty))
						{
							isAtLeastOneOverloadCancellable = true;
						}
					}
				}

				if (eligibleMethods.Count > 0 && !isAtLeastOneOverloadCancellable)
				{
					// Choose the method with the most parameters and then with the longest source code.
					IMethodSymbol method = eligibleMethods.OrderByDescending(m => m.Parameters.Length)
						.ThenBy(m => m.DeclaringSyntaxReferences.FirstOrDefault()?.Span.Length ?? 0)
						.First();
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

		private static IEnumerable<ITypeSymbol> GetTypeAndBaseTypes(ITypeSymbol type)
		{
			// https://stackoverflow.com/a/30445814/1882616
			ITypeSymbol? current = type;
			while (current != null)
			{
				yield return current;
				current = current.BaseType;
			}
		}

		private bool IsAwaitable(ITypeSymbol type)
		{
			bool result = false;

			if (this.fixedTaskTypes.Contains(type, SymbolComparer)
				|| (type is INamedTypeSymbol { IsGenericType: true } namedType
					&& this.genericTaskTypes.Contains(namedType.OriginalDefinition, SymbolComparer))
				|| type.GetAttributes().Any(attr => SymbolComparer.Equals(attr.AttributeClass, this.asyncMethodBuilderAttributeType)))
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
						if (method.Equals(interfaceImplementation, SymbolComparer))
						{
							result = true;
							break;
						}
					}
				}
			}

			return result;
		}

		private static bool IsAssemblyEntryPoint(IMethodSymbol method, ref SymbolAnalysisContext context)
		{
			bool result = method.IsStatic
				&& method.Name == "Main"
				&& SymbolComparer.Equals(method, context.Compilation.GetEntryPoint(context.CancellationToken));
			return result;
		}

		private bool HasCancellationTokenParameter(List<IParameterSymbol> parameters)
		{
			bool result = false;

			foreach (IParameterSymbol parameterSymbol in parameters)
			{
				INamedTypeSymbol? parameterType = parameterSymbol.Type as INamedTypeSymbol;
				if (this.cancellationTokenType.Equals(parameterType, SymbolComparer))
				{
					result = true;
					break;
				}
			}

			return result;
		}

		private bool ParameterHasCancellationTokenProperty(IParameterSymbol parameterSymbol)
		{
			ISet<string> propertyNamesToCheck = this.callingAnalyzer.Settings.PropertyNamesForCancellation;
			bool result = propertyNamesToCheck.Count > 0
				&& typeHasCancellationTokenProperty.GetOrAdd(parameterSymbol.Type, type =>
				{
					bool hasCancellationTokenProperty = false;
					ITypeSymbol[] typeHierarchy = [.. GetTypeAndBaseTypes(type)];
					IEnumerable<IPropertySymbol> publicCancellationProperties = propertyNamesToCheck
						.SelectMany(propertyName => typeHierarchy.SelectMany(t => t.GetMembers(propertyName)))
						.Where(m => m.Kind == SymbolKind.Property && m.DeclaredAccessibility == Accessibility.Public)
						.Cast<IPropertySymbol>();
					foreach (IPropertySymbol propertySymbol in publicCancellationProperties)
					{
						if (this.cancellationTokenType.Equals(propertySymbol.Type, SymbolComparer))
						{
							hasCancellationTokenProperty = true;
							break;
						}
					}

					return hasCancellationTokenProperty;
				});

			return result;
		}

		#endregion
	}

	#endregion
}
