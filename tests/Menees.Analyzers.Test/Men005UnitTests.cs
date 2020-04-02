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
	public sealed class Men005UnitTests : CodeFixVerifier
	{
		#region Protected Properties

		protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men005FileTooLong();

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
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Xml;
	using System.Xml.Linq;

	class TypeName
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public TypeName()
		{
			// This comment makes the file too long.
		}
	}
}";
			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "File Test0.cs must be no longer than 20 lines (now 21).",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 1) }
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}

		#endregion
	}
}