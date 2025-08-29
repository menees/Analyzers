namespace Menees.Analyzers.Test;

using Microsoft.CodeAnalysis.CodeFixes;

[TestClass]
public class Men019UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men019SupportAsyncCancellationToken();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable MEN019 // We have to make some 'invalid' interface methods inheritable.
public interface IThink1 { Task Think1(); }
public interface IThink2 { Task Think2(); }
#pragma warning restore MEN019

public sealed class Cancellable { public CancellationToken Cancel {get;}}
public sealed class TestBase : IThink1
{
	public virtual Task Think1() => return Task.CompletedTask;
}

public class Test : IFormattable, IThink2
{
	private Task<bool> CheckPrivate(CancellationToken c) { return Task.FromResult(true); }

	public async Task UseAsyncKeyword(CancellationToken c) { await Task.Yield(); }

	// Interface implementations
	Task IThink2.Think2() => return Task.CompletedTask;
	public string ToString(string? format, IFormatProvider? formatProvider) => ""test"";
	public override Task Think1() => return Task.CompletedTask;

	[TestMethod]
	public Task UnitTest() => return Task.CompletedTask;

	public Task GetsCancelProperty(Cancellable cancellable) => return Task.CompletedTask;
}";

		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTest

	[TestMethod]
	public void InvalidCodeTest()
	{
		const string test = @"
public class Test
{
	// todo: Add invalid methods
	// (method.DeclaredAccessibility > Accessibility.Private || settings.CheckPrivateMethodsForCancellation)
	// !method.IsImplicitlyDeclared
	// IsAsyncMethodKindSupported(method.MethodKind)
	// !method.ReturnsVoid
	// method.ReturnType is not null  // Roslyn's ISymbolExtensions.IsAwaitableNonDynamic checks this
	// (method.IsAsync || IsAwaitable(method.ReturnType))
	// !method.IsOverride
	// method.ExplicitInterfaceImplementations.IsDefaultOrEmpty
	// !IsImplicitInterfaceImplementation(method)
	// !settings.IsUnitTestMethod(method))
	// !HasCancellationTokenParameter(parameters)
	// !parameters.Any(ParameterHasCancellationTokenProperty)
}";

		// var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			// new DiagnosticResult(analyzer)
			// {
				// Message = "The numeric literal 1000000 should use digit separators.",
				// Locations = [new DiagnosticResultLocation("Test0.cs", 4, 30)]
			// },
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}