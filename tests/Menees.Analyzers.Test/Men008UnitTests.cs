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

			// These will use file names Test0.cs and Test1.cs.
			this.VerifyCSharpDiagnostic(new[] { @"partial class Test { }", @"partial class Test { }" });
			this.VerifyCSharpDiagnostic(new[] { @"partial struct Test { }", @"partial struct Test { }" });
			this.VerifyCSharpDiagnostic(new[] { @"partial interface Test { }", @"partial interface Test { }" });
			this.VerifyCSharpDiagnostic(new[] { @"class Test0 { }", @"class Test<T> { }" });

			string previousPrefix = DefaultFilePathPrefix;
			DefaultFilePathPrefix = "Test.aspx"; // This will use a file name of Test0.aspx.cs.
			try
			{
				this.VerifyCSharpDiagnostic(new[] { @"class Test { }" });
			}
			finally
			{
				DefaultFilePathPrefix = previousPrefix;
			}
		}

		#endregion

		#region InvalidCodeTestSingleType

		[TestMethod]
		public void InvalidCodeTestSingleType()
		{
			const string test = @"
namespace Testing
{
	class Invalid { }
}";
			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "File name Test0.cs doesn't match the name of a contained type.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2) }
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}

		#endregion

		#region InvalidCodeTestPartialType

		[TestMethod]
		public void InvalidCodeTestPartialType()
		{
			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "File name Test1.cs doesn't match the name of a contained type.",
					Locations = new[] { new DiagnosticResultLocation("Test1.cs", 2, 1) }
				},
			};

			string[] test = new[]
			{
				"partial class Test0 { }", // Test0.cs
				"//Line1\r\npartial class Test0 { }" // Test1.cs
			};
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
			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "File name Test0.cs doesn't exactly match type test0.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2) }
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}

		#endregion
	}
}