﻿namespace Menees.Analyzers.Test
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using Menees.Analyzers;
	using Menees.Analyzers.Test;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CodeFixes;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TestClass]
	public class Men014UnitTests : CodeFixVerifier
	{
		#region Protected Properties

		protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men014PreferTryGetValue();

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
	public Testing()
	{
		Dictionary<string, int> test = new();
		if (test.ContainsKey(""a""))
		{
			Debug.WriteLine(""Contains a"");
		}
	}
}";
			this.VerifyCSharpDiagnostic(test);
		}

		#endregion

		#region InvalidCodeTests

		[TestMethod]
		public void InvalidCodeLiteralKeyTest()
		{
			const string test = @"
using System.Collections.Generic;
using System.Diagnostics;
class Testing
{
	public Testing()
	{
		Dictionary<string, int> test = new();
		if (test.ContainsKey(""a""))
		{
			Debug.WriteLine(test[""a""]);
		}
	}
}";

			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "Use test.TryGetValue(\"a\", out var value) instead of test.ContainsKey(\"a\") and test[\"a\"].",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 12) },
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void InvalidCodeVariableKeyTest()
		{
			const string test = @"
using System.Collections.Generic;
using System.Diagnostics;
class Testing
{
	public Testing()
	{
		Dictionary<string, int> test = new();
		string key = ""a"";
		if (test.ContainsKey(key))
		{
			Debug.WriteLine(test[key]);
		}
	}
}";

			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "Use test.TryGetValue(key, out var value) instead of test.ContainsKey(key) and test[key].",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 12) },
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void InvalidCodeExpandedScopeTest()
		{
			const string test = @"
using System.Collections.Generic;
using System.Diagnostics;
class Testing
{
	public Testing()
	{
		Dictionary<string, int> test = new();
		string key = ""a"";
		bool found = test.ContainsKey(key);
		if (found)
		{
			Debug.WriteLine(test[key]);
		}
	}
}";

			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "Use test.TryGetValue(key, out var value) instead of test.ContainsKey(key) and test[key].",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 21) },
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void InvalidCodeConcurrentTest()
		{
			const string test = @"
using System.Collections.Concurrent;
using System.Diagnostics;
class Testing
{
	public Testing()
	{
		ConcurrentDictionary<string, int> test = new();
		string key = ""a"";
		if (test.ContainsKey(key))
		{
			Debug.WriteLine(test[key]);
		}
	}
}";

			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult(analyzer)
				{
					Message = "Use test.TryGetValue(key, out var value) instead of test.ContainsKey(key) and test[key].",
					Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 12) },
				},
			};

			this.VerifyCSharpDiagnostic(test, expected);
		}


		#endregion
	}
}