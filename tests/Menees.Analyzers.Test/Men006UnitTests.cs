namespace Menees.Analyzers.Test;

[TestClass]
public sealed class Men006UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men006RegionsShouldBeUsed();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		string test = @"
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

		test = @"
namespace Test
{
	#region Using Directives

	using System;

	#endregion

	class Testing
	{
		#region Constructors

		/// <summary>Test</summary>
		public Testing()
		{
		}

		#endregion

		#region Public Properties

		public string Name { get; set; }

		#endregion
	}
}";
		this.VerifyCSharpDiagnostic(test);

		test = @"
#region Using Directives
using System;
#endregion

#if NETFRAMEWORK
// For property ""init;"" support. https://stackoverflow.com/a/64749403/1882616
namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }
#endif

namespace Settings
{
	public record Test
	{
		#region Public Properties
		public string Name { get; init; } = string.Empty;
		public string Value { get; init; } = string.Empty;
		#endregion
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTestFileTooLong

	[TestMethod]
	public void InvalidCodeTestFileTooLong()
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
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "#regions should be used because Test0.cs is longer than 20 lines (now 21).",
				Locations = [new DiagnosticResultLocation("Test0.cs", 1, 1)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion

	#region InvalidCodeTestMultipleTypes

	[TestMethod]
	public void InvalidCodeTestMultipleTypes()
	{
		const string test = @"
namespace ConsoleApplication1
{
	class Type1
	{
		public Type1()
		{
		}
	}

	struct Type2
	{
		public string Name { get; set; }
	}
}";
		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "#regions should be used because Test0.cs contains multiple type declarations.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 1, 1)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion

	#region InvalidCodeTestPartialRegions

	[TestMethod]
	public void InvalidCodeTestClassPartialRegions()
	{
		const string test = @"
namespace ConsoleApplication1
{
	using System;

	class Type1
	{
		#region Constructors

		public Type1()
		{
		}

		#endregion

		public string Name { get; set; }
	}

	struct Type2
	{
		public string Name { get; set; }
	}
}";
		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "#regions should be used around using directives.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 4, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "#regions should be used around all members in Type1.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "#regions should be used around all members in Type2.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 19, 2)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void InvalidCodeTestRecordPartialRegions()
	{
		const string test = @"
namespace ConsoleApplication1
{
	using System;

	public record Type1
	{
		#region Constructors

		public Type1()
		{
		}

		#endregion

		public string Name { get; set; }
	}
}";
		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "#regions should be used around using directives.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 4, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "#regions should be used around all members in Type1.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 2)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}