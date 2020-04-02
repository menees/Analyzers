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
	public class Men009UnitTests : CodeFixVerifier
	{
		#region Protected Properties

		protected override CodeFixProvider CSharpCodeFixProvider => new Men009UsePreferredExceptionsFixer();

		protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men009UsePreferredExceptions();

		#endregion

		#region ValidCodeTest

		[TestMethod]
		public void ValidCodeTest()
		{
			this.VerifyCSharpDiagnostic(string.Empty);

			const string test = @"
class Testing
{
	public Testing()
	{
		if (Convert.ToBoolean(0))
		{
			throw new InvalidOperationException(""This shouldn't be reached."");
		}

		throw new NotSupportedException();
	}

	public Testing Create()
	{
		if (Convert.ToBoolean(0))
		{
			throw new NotSupportedException(""This shouldn't be reached."");
		}

		throw new System.NotSupportedException();
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
class Testing
{
	public Testing()
	{
		if (Convert.ToBoolean(0))
		{
			throw new InvalidOperationException(""This shouldn't be reached."");
		}

		throw new NotImplementedException();
	}

	public Testing Create()
	{
		if (Convert.ToBoolean(0))
		{
			throw new System
				.NotImplementedException(""This shouldn't be reached."");
		}

		throw new System.NotImplementedException();
	}

	public string Key { get { throw new NotImplementedException(""get""); } set { throw new NotImplementedException(""set""); } }

	public string Key2 { get => throw new NotImplementedException(""get2""); set => throw new NotImplementedException(""set2""); }
}";

			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "Use NotSupportedException instead of NotImplementedException.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 13) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Use NotSupportedException instead of NotImplementedException.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 6) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Use NotSupportedException instead of NotImplementedException.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 22, 20) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Use NotSupportedException instead of NotImplementedException.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 25, 38) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Use NotSupportedException instead of NotImplementedException.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 25, 88) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Use NotSupportedException instead of NotImplementedException.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 27, 40) }
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Use NotSupportedException instead of NotImplementedException.",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 27, 90) }
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);

			const string fixtest = @"
class Testing
{
	public Testing()
	{
		if (Convert.ToBoolean(0))
		{
			throw new InvalidOperationException(""This shouldn't be reached."");
		}

		throw new NotSupportedException();
	}

	public Testing Create()
	{
		if (Convert.ToBoolean(0))
		{
			throw new System
				.NotSupportedException(""This shouldn't be reached."");
		}

		throw new System.NotSupportedException();
	}

	public string Key { get { throw new NotSupportedException(""get""); } set { throw new NotSupportedException(""set""); } }

	public string Key2 { get => throw new NotSupportedException(""get2""); set => throw new NotSupportedException(""set2""); }
}";
			this.VerifyCSharpFix(test, fixtest);
		}

		#endregion
	}
}