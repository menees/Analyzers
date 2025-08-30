namespace Menees.Analyzers.Test;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;

[TestClass]
public class Men019UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men019SupportAsyncCancellationToken();

	protected override IEnumerable<Type> AssemblyRequiredTypes => [typeof(ValueTask), typeof(TestMethodAttribute)];

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable MEN019 // We have to make some 'invalid' interface methods inheritable.
public interface IThink1 { Task Think1(); }
public interface IThink2 { Task Think2(); }
#pragma warning restore MEN019

public sealed class Cancellable { public CancellationToken Cancel {get;}}
public class TestBase : IThink1
{
	public virtual Task Think1() => Task.CompletedTask;
}

public sealed class Awaitable { public Awaiter GetAwaiter() => new(); }
public sealed class Awaiter { public bool GetResult() => true; }

public class Test : TestBase, IFormattable, IThink2
{
	private Task<bool> CheckPrivate(CancellationToken c) { return Task.FromResult(true); }

	public async Task UseAsyncKeyword(CancellationToken c) { await Task.Yield(); }

	public Awaitable TryAwaitable(CancellationToken c = default) { return new Awaitable(); }

	// Interface implementations
	Task IThink2.Think2() => Task.CompletedTask;
	public string ToString(string? format, IFormatProvider? formatProvider) => ""test"";
	public override Task Think1() => Task.CompletedTask;

	[TestMethod]
	public Task UnitTest() => Task.CompletedTask;

	public Task GetsCancelProperty(Cancellable cancellable) => Task.CompletedTask;

	// Normal methods
	public override string ToString() => nameof(Test);
	public int GetPercent(double fraction) => (int)(100 * fraction);
	public void SyncCancel(CancellationToken c = default) { }
}";

		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTest

	[TestMethod]
	public void InvalidCodeTest()
	{
		const string test = @"#nullable enable
using System.Threading;
using System.Threading.Tasks;

public interface IThink1 { Task Think1(); }

public sealed class Cancellable { public CancellationToken Cancelled {get;}}

public class Test
{
	private Task<bool> CheckPrivate() { return Task.FromResult(true); }

	public async Task UseAsyncKeyword() { await Task.Yield(); }

	internal ValueTask<string?> GetString(int a, bool b, string c) { return new ValueTask<string?>(c); }

	// Not a configured name to look for.
	public Task GetsCancelledProperty(Cancellable cancellable) => Task.CompletedTask;
}";

		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Async method Think1 should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 5, 33)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method CheckPrivate should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 11, 21)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method UseAsyncKeyword should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 13, 20)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method GetString should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 15, 30)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method GetsCancelledProperty should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 18, 14)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}