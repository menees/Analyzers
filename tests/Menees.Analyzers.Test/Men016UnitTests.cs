namespace Menees.Analyzers.Test;

[TestClass]
public class Men016UnitTests : CodeFixVerifier
{
	#region Private Data Members

	private const string ExpectedMessage = "Use object-oriented methods instead of top-level statements.";

	#endregion

	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men016AvoidTopLevelStatements();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"
using System.Collections.Generic;
using System.Diagnostics;
class Testing
{
	Dictionary<string, string> _entries = new();

	public Testing()
	{
		Dictionary<string, int> test = new();
		if (test.ContainsKey(""a""))
		{
			Debug.WriteLine(""Contains a"");
		}
	}

	public void Mask()
	{
		if (_entries.ContainsKey(""Password""))
		{
			_entries[""Password""] = ""********"";
		}
	}

	public bool ContainsKey(string key) => _entries.ContainsKey(key);
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTests

	[TestMethod]
	public void InvalidCodeSimpleTest()
	{
		const string test = @"
using System;
Console.WriteLine(""Test"");
";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = ExpectedMessage,
				Locations = [new DiagnosticResultLocation("Test0.cs", 3, 1)],
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void InvalidCodeWithClassTest()
	{
		const string test = @"
using System;
using static System.Console;

WriteLine(""Test"");
MyClass.TestMethod();

public class MyClass
{
	public static void TestMethod()
	{
		Console.WriteLine(""Hello World!"");
	}
}
";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = ExpectedMessage,
				Locations = [new DiagnosticResultLocation("Test0.cs", 5, 1)],
			},
			new DiagnosticResult(analyzer)
			{
				Message = ExpectedMessage,
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 1)],
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void InvalidCodeWithNamespaceTest()
	{
		const string test = @"
using System;

MyNamespace.MyClass.MyMethod();

namespace MyNamespace
{
	class MyClass
	{
		public static void MyMethod()
		{
			Console.WriteLine(""Hello World from MyNamespace.MyClass.MyMethod!"");
		}
	}
}";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = ExpectedMessage,
				Locations = [new DiagnosticResultLocation("Test0.cs", 4, 1)],
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}


	#endregion
}