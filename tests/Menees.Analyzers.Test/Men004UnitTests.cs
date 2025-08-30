namespace Menees.Analyzers.Test;

[TestClass]
public class Men004UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men004PropertyAccessorTooLong();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"using System;
class Testing
{
	/// <summary>Test</summary>
	public Testing()
	{
	}

	public DateTime Now
	{
		get
		{
			return DateTime.Now;
		}
	}

	public event EventHandler Changed
	{
		add
		{
		}

		remove
		{
		}
	}

	public int this[int index]
	{
		get
		{
			return index;
		}
	}

	public Testing Create()
	{
		return new Testing();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTest

	[TestMethod]
	public void InvalidCodeTest()
	{
		const string test = @"using System;
class Testing
{
	public DateTime Now
	{
		get
		{
			// Line
			// Line
			// Line
			// Line
			return DateTime.Now;
		}

		set
		{
			// Line
			// Line
			// Line
			// Line
		}
	}

	public event EventHandler Changed
	{
		add
		{
			// Line
			// Line
			// Line
			// Line
		}

		remove
		{
			// Line
			// Line
			// Line
			// Line
		}
	}

	public int this[int index]
	{
		get
		{
			// Line
			// Line
			// Line
			// Line
			return index;
		}
	}

	// These methods should be ignored by Men004PropertyAccessorTooLong.
	public Testing()
	{
		// Line
		// Line
		// Line
		// Line
	}

	public Testing Create()
	{
		// Line
		// Line
		// Line
		return new Testing();
	}
}";
		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Property Now get accessor must be no longer than 5 lines (now 8).",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 3)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Property Now set accessor must be no longer than 5 lines (now 7).",
				Locations = [new DiagnosticResultLocation("Test0.cs", 15, 3)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Event Changed add accessor must be no longer than 5 lines (now 7).",
				Locations = [new DiagnosticResultLocation("Test0.cs", 26, 3)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Event Changed remove accessor must be no longer than 5 lines (now 7).",
				Locations = [new DiagnosticResultLocation("Test0.cs", 34, 3)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Property Item get accessor must be no longer than 5 lines (now 8).",
				Locations = [new DiagnosticResultLocation("Test0.cs", 45, 3)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}