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
	public sealed class Men002UnitTests : CodeFixVerifier
	{
		#region Protected Properties

		protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men002LineTooLong();

		#endregion

		#region ValidCodeTest

		[TestMethod]
		public void ValidCodeTest()
		{
			this.VerifyCSharpDiagnostic(string.Empty);

			const string test = @"
using System;

namespace Test
{
	class Testing
	{
		/// <summary>Test</summary>
		public Testing()
		{
		}
	}
}";
			this.VerifyCSharpDiagnostic(test);
		}

		#endregion

		#region InvalidCodeTest

		[TestMethod]
		public void InvalidCodeTest()
		{
			const string test = @"
namespace ConsoleApplication1
{
	using System;
	using System.Collections.Generic; // This line runs on much much much too long.

	class TypeName
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public TypeName() // This line is also much too long.
		{
		}
	}
}";
			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "Line must be no longer than 40 characters (now 83).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 5, 38) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Line must be no longer than 40 characters (now 61).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 35) }
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}

		#endregion
	}
}