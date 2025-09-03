namespace Menees.Analyzers.Test;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;

[TestClass]
public class Men019UnitTests : CodeFixVerifier
{
	#region Private Data Members

	private const string SharedCode = """

		public sealed class Awaitable { public Awaiter GetAwaiter() => new(); }
		public sealed class Awaiter { public bool GetResult() => true; }

		[AsyncMethodBuilder(typeof(MyTaskBuilder))]
		public readonly struct MyTask
		{
		}

		class MyTaskBuilder
		{
		    public static MyTaskBuilder Create() => new();
		    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
		    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
		    public void SetResult() { }
		    public void SetException(Exception exception) { }
		    public MyTask Task => default(MyTask);
		    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
		    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
		}
		""";

	private const string InvalidCode = """
		#nullable enable
		using System;
		using System.Runtime.CompilerServices;
		using System.Threading;
		using System.Threading.Tasks;

		public interface IThink1 { Task Think1(); }

		public sealed class Cancellable { public CancellationToken Cancelled {get;}}

		public class Test
		{
			private Task<bool> CheckPrivate() { return Task.FromResult(true); }

			public async Task UseAsyncKeyword() { await Task.Yield(); }

			internal ValueTask<string?> GetString(int a, bool b, string? c = null) { return new ValueTask<string?>(c); }

			// Not a configured name to look for.
			public Task GetsCancelledProperty(Cancellable cancellable) => Task.CompletedTask;

			public Task LocalMethodGroup(string name) => Task.CompletedTask;
			public Task LocalMethodGroup(string name, int i) => Task.CompletedTask;
			public Task LocalMethodGroup(string name, int i, params string[] tokens) => Task.CompletedTask;

			public Awaitable TryAwaitable() { return new Awaitable(); }

			public MyTask UseMyTask() => new();
		}
		""" + SharedCode;

	#endregion

	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men019SupportAsyncCancellationToken();

	protected override CodeFixProvider? CSharpCodeFixProvider => new Men019SupportAsyncCancellationTokenFixer();

	protected override IEnumerable<Type> AssemblyRequiredTypes => [typeof(ValueTask), typeof(TestMethodAttribute)];

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		string test = @"#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable MEN019 // We have to make some 'invalid' interface methods inheritable.
public interface IThink1 { Task Think1(); }
public interface IThink2 { Task Think2(); }
#pragma warning restore MEN019

public class Cancellable { public CancellationToken Cancel {get;}}
public sealed class DerivedCancellable : Cancellable { public bool IsDerived {get;} = true; }

public class TestBase : IThink1
{
	public virtual Task Think1() => Task.CompletedTask;

	public Task SplitMethodGroup(CancellationToken c) => Task.CompletedTask;
	public int Ineligible(int i) => i;
}

interface InterfaceName
{
  Task MethodAsync();
  Task MethodAsync(CancellationToken cancellationToken);
}

public class Test : TestBase, IFormattable, IThink2
{
	private Task<bool> CheckPrivate(CancellationToken c) { return Task.FromResult(true); }

	public async Task UseAsyncKeyword(CancellationToken c) { await Task.Yield(); }

	public Awaitable TryAwaitable(CancellationToken c = default) { return new Awaitable(); }

	public MyTask UseMyTask(CancellationToken c) => new();

	// Interface implementations
	Task IThink2.Think2() => Task.CompletedTask;
	public string ToString(string? format, IFormatProvider? formatProvider) => ""test"";
	public override Task Think1() => Task.CompletedTask;

	[TestMethod]
	public Task UnitTest() => Task.CompletedTask;

	public Task GetsCancelProperty(Cancellable cancellable) => Task.CompletedTask;
	public Task GetsInheritedCancelProperty(DerivedCancellable cancellable) => Task.CompletedTask;

	// Normal methods
	public override string ToString() => nameof(Test);
	public int GetPercent(double fraction) => (int)(100 * fraction);
	public void SyncCancel(CancellationToken c = default) { }

	public Task LocalMethodGroup(string name) => Task.CompletedTask;
	public Task LocalMethodGroup(string name, CancellationToken c) => Task.CompletedTask;

	public Task SplitMethodGroup() => Task.CompletedTask; // Base class's overload is cancellable.
	public int Ineligible(int i, int multiplier) => i * multiplier;
}"
+ Environment.NewLine + SharedCode;

		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTest

	[TestMethod]
	public void InvalidCodeTest()
	{
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Async method Think1 should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 33)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method CheckPrivate should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 13, 21)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method UseAsyncKeyword should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 15, 20)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method GetString should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 17, 30)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method GetsCancelledProperty should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 20, 14)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method LocalMethodGroup should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 24, 14)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method TryAwaitable should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 26, 19)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Async method UseMyTask should take a CancellationToken parameter.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 28, 16)]
			},
		];

		this.VerifyCSharpDiagnostic(InvalidCode, expected);
	}

	[TestMethod]
	public void InvalidCodeFixerTestWithoutDefault()
	{
		const string after = """
			#nullable enable
			using System;
			using System.Runtime.CompilerServices;
			using System.Threading;
			using System.Threading.Tasks;

			public interface IThink1 { Task Think1(CancellationToken cancellationToken); }

			public sealed class Cancellable { public CancellationToken Cancelled {get;}}

			public class Test
			{
				private Task<bool> CheckPrivate(CancellationToken cancellationToken) { return Task.FromResult(true); }

				public async Task UseAsyncKeyword(CancellationToken cancellationToken) { await Task.Yield(); }

				internal ValueTask<string?> GetString(int a, bool b, CancellationToken cancellationToken, string? c = null) { return new ValueTask<string?>(c); }

				// Not a configured name to look for.
				public Task GetsCancelledProperty(Cancellable cancellable, CancellationToken cancellationToken) => Task.CompletedTask;

				public Task LocalMethodGroup(string name) => Task.CompletedTask;
				public Task LocalMethodGroup(string name, int i) => Task.CompletedTask;
				public Task LocalMethodGroup(string name, int i, CancellationToken cancellationToken, params string[] tokens) => Task.CompletedTask;

				public Awaitable TryAwaitable(CancellationToken cancellationToken) { return new Awaitable(); }

				public MyTask UseMyTask(CancellationToken cancellationToken) => new();
			}
			""" + SharedCode;

		this.VerifyCSharpFix(InvalidCode, after, codeFixIndex: 0);
	}

	[TestMethod]
	public void InvalidCodeFixerTestWithDefault()
	{
		const string after = """
			#nullable enable
			using System;
			using System.Runtime.CompilerServices;
			using System.Threading;
			using System.Threading.Tasks;

			public interface IThink1 { Task Think1(CancellationToken cancellationToken = default); }

			public sealed class Cancellable { public CancellationToken Cancelled {get;}}

			public class Test
			{
				private Task<bool> CheckPrivate(CancellationToken cancellationToken = default) { return Task.FromResult(true); }

				public async Task UseAsyncKeyword(CancellationToken cancellationToken = default) { await Task.Yield(); }

				internal ValueTask<string?> GetString(int a, bool b, string? c = null, CancellationToken cancellationToken = default) { return new ValueTask<string?>(c); }

				// Not a configured name to look for.
				public Task GetsCancelledProperty(Cancellable cancellable, CancellationToken cancellationToken = default) => Task.CompletedTask;

				public Task LocalMethodGroup(string name) => Task.CompletedTask;
				public Task LocalMethodGroup(string name, int i) => Task.CompletedTask;
				public Task LocalMethodGroup(string name, int i, CancellationToken cancellationToken = default, params string[] tokens) => Task.CompletedTask;

				public Awaitable TryAwaitable(CancellationToken cancellationToken = default) { return new Awaitable(); }

				public MyTask UseMyTask(CancellationToken cancellationToken = default) => new();
			}
			""" + SharedCode;

		this.VerifyCSharpFix(InvalidCode, after, codeFixIndex: 1);
	}

	#endregion
}