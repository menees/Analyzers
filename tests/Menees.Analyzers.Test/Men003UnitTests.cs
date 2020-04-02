namespace Menees.Analyzers.Test
{
	#region Using Directives

	using System;
	using Menees.Analyzers;
	using Menees.Analyzers.Test;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CodeFixes;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TestClass]
	public class Men003UnitTests : CodeFixVerifier
	{
		#region Private Data Members

		// I had to factor this out into a const member to keep the InvalidCodeTest method less than 120 lines.
		private const string InvalidCode = @"
class Testing
{
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

	public Testing Create2()
	{
		return new Testing();
	}

	static Testing()
	{
		// Line
		// Line
		// Line
		// Line
	}

	~Testing()
	{
		// Line
		// Line
		// Line
		// Line
	}

	public DateTime Now
	{
		// This property accessor should be ignored by Men003MethodTooLong.
		get
		{
			// Line
			// Line
			// Line
			// Line
			return DateTime.Now;
		}
	}

	public static explicit operator Testing(byte b)
	{
		// Line
		// Line
		// Line
		// Line
		return Create();
	}

	public static implicit operator byte(Testing d)
	{
		// Line
		// Line
		// Line
		// Line
		return 1;
	}

	public static bool operator ==(Testing x, Testing y)
	{
		// Line
		// Line
		// Line
		// Line
		return true;
	}

	public static bool operator !=(Testing x, Testing y)
	{
		// Line
		// Line
		// Line
		// Line
		return false;
	}
}";

		#endregion

		#region Protected Properties

		protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men003MethodTooLong();

		#endregion

		#region ValidCodeTest

		[TestMethod]
		public void ValidCodeTest()
		{
			this.VerifyCSharpDiagnostic(string.Empty);

			const string test = @"
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
			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "Constructor Testing must be no longer than 5 lines (now 7).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Method Create must be no longer than 5 lines (now 7).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 2) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Static constructor Testing must be no longer than 5 lines (now 7).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 25, 2) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Destructor ~Testing must be no longer than 5 lines (now 7).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 33, 2) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Operator Explicit must be no longer than 5 lines (now 8).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 54, 2) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Operator Implicit must be no longer than 5 lines (now 8).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 63, 2) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Operator Equality must be no longer than 5 lines (now 8).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 72, 2) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Operator Inequality must be no longer than 5 lines (now 8).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 81, 2) }
				},
			};

			this.VerifyCSharpDiagnostic(InvalidCode, expected);
		}

		#endregion
	}
}