namespace Menees.Analyzers.Test;

[TestClass]
public sealed class Men008UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men008FileNameShouldMatchType();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"
namespace ValidCode
{
	class Test0
	{
		public Test0()
		{
		}

		private class Nested { }
	}
}";

		// These will use file name Test0.cs.
		this.VerifyCSharpDiagnostic(test);
		this.VerifyCSharpDiagnostic("namespace X { class Test0 { } }");
		this.VerifyCSharpDiagnostic("class Outer { class Test0 { } }");
		this.VerifyCSharpDiagnostic("namespace X { record Test0 { } }");
		this.VerifyCSharpDiagnostic("record Outer { record Test0 { } }");
		this.VerifyCSharpDiagnostic("namespace X { struct Test0 { } }");
		this.VerifyCSharpDiagnostic("struct Outer { struct Test0 { } }");

		// These will use file names Test0.cs and Test1.cs.
#pragma warning disable CA1861 // Avoid constant arrays as arguments. This is ok in a unit test.
		this.VerifyCSharpDiagnostic(new[] { @"partial class Test { }", @"partial class Test { }" });
		this.VerifyCSharpDiagnostic(new[] { @"partial struct Test { }", @"partial struct Test { }" });
		this.VerifyCSharpDiagnostic(new[] { @"partial interface Test { }", @"partial interface Test { }" });
		this.VerifyCSharpDiagnostic(new[] { @"class Test0 { }", @"class Test<T> { }" });

		string previousPrefix = DefaultFilePathPrefix.Value ?? string.Empty;
		DefaultFilePathPrefix.Value = "Test.aspx"; // This will use a file name of Test0.aspx.cs.
		try
		{
			this.VerifyCSharpDiagnostic(new[] { @"class Test { }" });
#pragma warning restore CA1861 // Avoid constant arrays as arguments
		}
		finally
		{
			DefaultFilePathPrefix.Value = previousPrefix;
		}
	}

	#endregion

	#region InvalidCodeTestSingleType

	[TestMethod]
	public void InvalidCodeTestSingleType()
	{
		foreach (string declarationType in new[] { "class", "struct", "record" })
		{
			string test = @"namespace Testing {" + declarationType + " Invalid { } }";
			DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected =
			[
				new DiagnosticResult(analyzer)
				{
					Message = "File name Test0.cs doesn't match the name of a contained type.",
					Locations = [new DiagnosticResultLocation("Test0.cs", 1, 20)]
				},
			];

			this.VerifyCSharpDiagnostic(test, expected);
		}
	}

	#endregion

	#region InvalidCodeTestPartialType

	[TestMethod]
	public void InvalidCodeTestPartialType()
	{
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "File name Test1.cs doesn't match the name of a contained type.",
				Locations = [new DiagnosticResultLocation("Test1.cs", 2, 1)]
			},
		];

		string[] test =
		[
			"partial class Test0 { }", // Test0.cs
			"//Line1\r\npartial class Test0 { }" // Test1.cs
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion

	#region InvalidCodeTestNearMatch

	[TestMethod]
	public void InvalidCodeTestNearMatch()
	{
		const string test = @"
namespace Testing
{
	class test0 { }
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "File name Test0.cs doesn't exactly match type test0.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 4, 2)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}