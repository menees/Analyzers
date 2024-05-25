namespace Menees.Analyzers.Test;

[TestClass]
public class Men015UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override CodeFixProvider CSharpCodeFixProvider => new Men015UsePreferredTermsFixer();

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men015UsePreferredTerms();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"
using System;
class Canceled
{
	public int Color => 1;
	public int Custom => 2;

	public Canceled()
	{
		int indexes = 0;
		Console.WriteLine(indexes);
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTests

	[TestMethod]
	public void InvalidCodeReportTest()
	{
		const string test = @"
namespace TestID.Kustom
{
	class Cancelled
	{
		private int colourID;
		public Cancelled()
		{
			colourID = GetIndicies();
		}

		public int Kustom => colourID;

		private int GetIndices() => 1;
	}
}";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use TestId instead of TestID.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 2, 11)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "Id" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use Custom instead of Kustom.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 2, 18)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "Custom" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use Canceled instead of Cancelled.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 4, 8)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "Canceled" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use colorId instead of colourID.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 15)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "color" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use Custom instead of Kustom.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 12, 14)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "Custom" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use GetIndexes instead of GetIndices.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 14, 15)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "Custom" } }
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void InvalidCodeFixTest()
	{
		// Due to limitations of the VerifyCSharpFix method, we can't text a mix of fixable and non-fixable identifiers.
		// VerifyCSharpFix assumes that every diagnostic reported can be fixed.
		const string test = @"
class Cancelled
{
	private int colourID;
	public Cancelled()
	{
		colourID = GetIndices();
	}

	public int Kustom => colourID;

	private int GetIndices() => 1;
}";

		const string fixtest = @"
class Canceled
{
	private int colorId;
	public Canceled()
	{
		colorId = GetIndexes();
	}

	public int Custom => colorId;

	private int GetIndexes() => 1;
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void InvalidCodeIgnoreOverrideTest()
	{
		const string test = @"
namespace Testing;
class Base
{
	public virtual int Colour => 1;
	public virtual int GetIndices() => 1;
}
class Derived : Base
{
	public override int Colour => 2;
	public override int GetIndices() => 2;
}
";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use Color instead of Colour.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 5, 21)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "Color" } }
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Use GetIndexes instead of GetIndices.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 21)],
				Properties = new Dictionary<string, string>() { { Men015UsePreferredTerms.PreferredKey, "Indexes" } }
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion

	#region SplitIntoTerms Tests

	[TestMethod]
	public void SplitIntoTermsTest()
	{
		Test("id", "id");
		Test("id", "id");
		Test("SystemId", "System", "Id");
		Test("SystemID", "System", "ID");
		Test("textBox", "text", "Box");
		Test("TextBox", "Text", "Box");
		Test("MFCTest", "MFC", "Test");
		Test("GetIISServerName", "Get", "IIS", "Server", "Name");
		Test("CString", "C", "String");
		Test("SIP911", "SIP", "911");
		Test("Farenheit451", "Farenheit", "451");
		Test("my_token", "my", "_", "token");
		Test("longLiveMTV", "long", "Live", "MTV");

		static void Test(string text, params string[] expected)
		{
			string[] actual = Men015UsePreferredTerms.SplitIntoTerms(text);
			CollectionAssert.AreEqual(expected, actual, text);
		}
	}

	#endregion
}