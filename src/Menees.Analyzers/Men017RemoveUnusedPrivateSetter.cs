namespace Menees.Analyzers;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

// This was inspired by Dustin Campbell's UseGetterOnlyAutoPropertyAnalyzer, which was abandoned in 2015.
// https://github.com/DustinCampbell/CSharpEssentials/blob/master/Source/CSharpEssentials/GetterOnlyAutoProperty/UseGetterOnlyAutoPropertyAnalyzer.cs
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Men017RemoveUnusedPrivateSetter : Analyzer
{
	#region Public Constants

	public const string DiagnosticId = "MEN017";

	#endregion

	#region Private Data Members

	private static readonly LocalizableString Title =
		new LocalizableResourceString(nameof(Resources.Men017Title), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString MessageFormat =
		new LocalizableResourceString(nameof(Resources.Men017MessageFormat), Resources.ResourceManager, typeof(Resources));

	private static readonly LocalizableString Description =
		new LocalizableResourceString(nameof(Resources.Men017Description), Resources.ResourceManager, typeof(Resources));

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

		// With some work we could handle SyntaxKind.InitAccessorDeclaration too, but I've never seen
		// a "private init" accessor used. It's possible, but it's rare enough that I'm going to ignore
		// it. The compiler ensures a private init accessor is only used by a constructor or by an
		// object initializer within the same type, so it's not worth the extra complexity to flag
		// places where a private init accessor is really only used from a constructor.
		context.RegisterSyntaxNodeActionHonorExclusions(this, HandleSetter, SyntaxKind.SetAccessorDeclaration);
	}

	#endregion

	#region Private Methods

	private static void HandleSetter(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is AccessorDeclarationSyntax accessorDeclaration
			&& accessorDeclaration.Parent is AccessorListSyntax accessorList
			&& accessorList.Parent is PropertyDeclarationSyntax propertyDeclaration)
		{
			// We have to use GetDeclaredSymbol (not GetSymbolInfo) since we're looking for DeclarationSyntax-related symbols.
			// https://stackoverflow.com/questions/33492992/when-to-use-semanticmodel-getsymbolinfo-and-when-semanticmodel-getdeclaredsymbol#comment54798428_33493739
			//
			// We'll ignore properties that have attributes because those imply reflection usage.
			// Specifically, there are a lot of attributes related to serialization that might mean
			// a setter is needed (even if it's private since reflection can ignore that). For example,
			// WCF's [DataMember] or ProtoBuf-Net's [ProtoMember].
			SemanticModel model = context.SemanticModel;
			if (model.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) is IPropertySymbol propertyDeclarationSymbol
				&& !propertyDeclarationSymbol.IsReadOnly
				&& propertyDeclarationSymbol.ExplicitInterfaceImplementations.IsDefaultOrEmpty // Can't change interfaces
				&& propertyDeclarationSymbol.SetMethod is IMethodSymbol setMethod
				&& setMethod.DeclaredAccessibility == Accessibility.Private
				&& accessorDeclaration.Body is null // Auto-property accessors have no body.
				&& propertyDeclarationSymbol.GetAttributes().Length == 0 /* See Reflection note above*/)
			{
				bool canRemove = true;

				// Look for any assignments to this property within the current type but outside of its constructors.
				// Partial types can be spread across multiple SyntaxReferences.
				INamedTypeSymbol namedType = propertyDeclarationSymbol.ContainingType;
				foreach (SyntaxReference typeReference in namedType.DeclaringSyntaxReferences)
				{
					SyntaxNode typeNode = typeReference.GetSyntax(context.CancellationToken);
					IEnumerable<SyntaxNode> identifierNodes = typeNode.DescendantNodes().Where(node => node.IsKind(SyntaxKind.IdentifierName));
					foreach (SyntaxNode identifierNode in identifierNodes)
					{
						// Is this identifier for the property declaration we're checking?
						// Is the identifier used on the left-hand side of an assignment statement?
						// Is this assignment being done outside of a constructor?
						if (model.GetSymbolInfo(identifierNode, context.CancellationToken).Symbol is IPropertySymbol property
							&& SymbolEqualityComparer.Default.Equals(property, propertyDeclarationSymbol)
							&& TryGetAssignmentExpression(identifierNode) is SyntaxNode assignmentExpression
							&& model.GetSymbolInfo(assignmentExpression, context.CancellationToken).Symbol is ISymbol assignedSymbol
							&& SymbolEqualityComparer.Default.Equals(assignedSymbol, property)
							&& !IsInConstructor(assignmentExpression, property.ContainingType, property.IsStatic, model, context.CancellationToken))
						{
							canRemove = false;
							break;
						}
					}

					if (!canRemove)
					{
						break;
					}
				}

				// Note: With a primary constructor, there won't be a reference to a private setter
				// because it will use property initializer syntax.
				if (canRemove)
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, accessorDeclaration.GetLocation(), propertyDeclaration.Identifier);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}

	private static SyntaxNode? TryGetAssignmentExpression(SyntaxNode identifierNode)
	{
		SyntaxNode? result = null;
		bool finished = false;

		for (SyntaxNode? node = identifierNode.Parent; node != null && result == null && !finished; node = node.Parent)
		{
			switch (node.Kind())
			{
				// Simple or compound assignment
				case SyntaxKind.SimpleAssignmentExpression:
				case SyntaxKind.OrAssignmentExpression:
				case SyntaxKind.AndAssignmentExpression:
				case SyntaxKind.ExclusiveOrAssignmentExpression:
				case SyntaxKind.AddAssignmentExpression:
				case SyntaxKind.SubtractAssignmentExpression:
				case SyntaxKind.MultiplyAssignmentExpression:
				case SyntaxKind.DivideAssignmentExpression:
				case SyntaxKind.ModuloAssignmentExpression:
				case SyntaxKind.LeftShiftAssignmentExpression:
				case SyntaxKind.RightShiftAssignmentExpression:
				case SyntaxKind.CoalesceAssignmentExpression:
					AssignmentExpressionSyntax assignment = (AssignmentExpressionSyntax)node;
					result = assignment.Left;
					break;

				// Prefix unary expression
				case SyntaxKind.PreIncrementExpression:
				case SyntaxKind.PreDecrementExpression:
					result = ((PrefixUnaryExpressionSyntax)node).Operand;
					break;

				// Postfix unary expression
				case SyntaxKind.PostIncrementExpression:
				case SyntaxKind.PostDecrementExpression:
					result = ((PostfixUnaryExpressionSyntax)node).Operand;
					break;

				// Early loop termination
				case SyntaxKind.Block:
				case SyntaxKind.ExpressionStatement:
					result = null;
					finished = true;
					break;
			}
		}

		return result;
	}

	private static bool IsInConstructor(
		SyntaxNode assignmentNode,
		INamedTypeSymbol type,
		bool isPropertyStatic,
		SemanticModel model,
		CancellationToken cancellation)
	{
		bool? result = null;

		for (SyntaxNode? node = assignmentNode; node != null && result == null; node = node.Parent)
		{
			switch (node.Kind())
			{
				// If it's the constructor for the type that contains the property, we're done with a true result.
				// If it's the constructor for another type (e.g., nested), we're done with a false result.
				case SyntaxKind.ConstructorDeclaration:
					ISymbol? constructorSymbol = model.GetDeclaredSymbol(node, cancellation);
					result = constructorSymbol != null
						&& SymbolEqualityComparer.Default.Equals(constructorSymbol.ContainingType, type)
						&& isPropertyStatic == constructorSymbol.IsStatic;
					break;

				// If it's in a lambda or local function, the compiler considers it a non-constructor use.
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.LocalFunctionStatement:
					result = false;
					break;

				// If we walk up the hierarchy and hit these type or member declarations, then we can quit early.
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.StructDeclaration:
				case SyntaxKind.RecordDeclaration:
				case SyntaxKind.MethodDeclaration:
				case SyntaxKind.PropertyDeclaration:
					result = false;
					break;
			}
		}

		return result ?? false;
	}

	#endregion
}
