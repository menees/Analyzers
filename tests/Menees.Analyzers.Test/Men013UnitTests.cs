namespace Menees.Analyzers.Test;

[TestClass]
public class Men013UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override CodeFixProvider CSharpCodeFixProvider => new Men013UseUtcTimeFixer();

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men013UseUtcTime();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"
using System;
class Testing
{
	public int Today => 1;
	public int Now => 2;

	public Testing()
	{
		if (Now == Today)
			Convert.ToString(1);
		else
			DateTime.UtcNow.ToString();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTests

	[TestMethod]
	public void InvalidCodeFullyQualifiedTest()
	{
		const string test = @"
class Testing
{
	public Testing()
	{
		if (System.DateTime.Now == System.DateTime.MinValue
			|| System.DateTime.Today == System.DateTime.MinValue)
		{
			System.DateTime.Now.ToString();
			System.DateTime.Today.ToString();
		}
	}
}";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow instead of Now.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 23)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow.Date instead of Today.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 23)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow instead of Now.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 9, 20)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow.Date instead of Today.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 10, 20)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);

		const string fixtest = @"
class Testing
{
	public Testing()
	{
		if (System.DateTime.UtcNow == System.DateTime.MinValue
			|| System.DateTime.UtcNow.Date == System.DateTime.MinValue)
		{
			System.DateTime.UtcNow.ToString();
			System.DateTime.UtcNow.Date.ToString();
		}
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void InvalidCodeUsingSystemTest()
	{
		const string test = @"
using System;
class Testing
{
	public Testing()
	{
		if (DateTime.Now == DateTime.MinValue
			|| DateTime.Today == DateTime.MinValue)
		{
			DateTime.Now.ToString();
			DateTime.Today.ToString();
		}
	}
}";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow instead of Now.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 16)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow.Date instead of Today.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 16)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow instead of Now.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 10, 13)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow.Date instead of Today.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 11, 13)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);

		const string fixtest = @"
using System;
class Testing
{
	public Testing()
	{
		if (DateTime.UtcNow == DateTime.MinValue
			|| DateTime.UtcNow.Date == DateTime.MinValue)
		{
			DateTime.UtcNow.ToString();
			DateTime.UtcNow.Date.ToString();
		}
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void InvalidCodeUsingStaticTest()
	{
		const string diagnosticsTest = @"using System;
using static System.DateTime;
class Testing
{
	public Testing()
	{
		if (Now > DateTime.MinValue
			|| Today < DateTime.MaxValue)
		{
			Now.ToString();
			var x = Now;
			x.ToString();
			Today.ToString();
			x = Today;
			x.ToString();
		}
	}
}";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow instead of Now.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 7)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow.Date instead of Today.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 7)],
				Properties = new Dictionary<string, string>() { { "CanFix", "False" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow instead of Now.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 10, 4)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow instead of Now.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 11, 12)],
				Properties = new Dictionary<string, string>() { { "CanFix", "True" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow.Date instead of Today.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 13, 4)],
				Properties = new Dictionary<string, string>() { { "CanFix", "False" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use UtcNow.Date instead of Today.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 14, 8)],
				Properties = new Dictionary<string, string>() { { "CanFix", "False" } }
			},
		];

		this.VerifyCSharpDiagnostic(diagnosticsTest, expected);

		// NOTE: The base CodeFixVerifier's VerifyCSharpFix method can't validate correctly when some
		// diagnostics are fixed and some are ignored.  It re-gets the latest diagnostics after applying
		// each one to the document, and it stops once a diagnostic reports no fix action even if other
		// diagnostics exist that haven't been applied!  The simplest workaround is to not mix fixable
		// and non-fixable conditions in the same VerifyCSharpFix call.
		const string nowTest = @"using System;
using static System.DateTime;
class Testing
{
	public Testing()
	{
		if (Now > DateTime.MinValue)
		{
			Now.ToString();
			var x = Now;
			x.ToString();
		}
	}
}";
		const string fixNowTest = @"using System;
using static System.DateTime;
class Testing
{
	public Testing()
	{
		if (UtcNow > DateTime.MinValue)
		{
			UtcNow.ToString();
			var x = UtcNow;
			x.ToString();
		}
	}
}";
		this.VerifyCSharpFix(nowTest, fixNowTest);

		const string todayTestCannotBeFixed = @"using System;
using static System.DateTime;
class Testing
{
	public Testing()
	{
		if (Today < DateTime.MaxValue)
		{
			Today.ToString();
			var x = Today;
			x.ToString();
		}
	}
}";
		this.VerifyCSharpFix(todayTestCannotBeFixed, todayTestCannotBeFixed);
	}

	#endregion
}