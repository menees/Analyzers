namespace Menees.Analyzers.Test;

[TestClass]
public sealed class Men001UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override CodeFixProvider CSharpCodeFixProvider => new Men001TabsShouldBeUsedFixer();

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men001TabsShouldBeUsed();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"	// On first line
using System;

namespace Test
{
	class TypeName
	{
		/// <summary>Constructor</summary>
		/// <param name=""value1"">Sample value</param>
		/// <param name=""value2"">Another sample value</param>
		public TypeName(int value1, string value2)
		{
#pragma warning disable 1234
			/* The space between these comments... */ /* ...should not be a problem. */
#pragma warning restore 1234
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
		const string SpaceTab = "    ";
		const string test = SpaceTab + @"// On first line
namespace ConsoleApplication1
{
	using System;
" + SpaceTab + @"using System.Collections.Generic;

	class TypeName
	{
		/// <summary>
	" + SpaceTab + @"/// Constructor
		/// </summary>
	" + SpaceTab + @"public TypeName()
" + SpaceTab + SpaceTab + @"{
		}
	}
}";
		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 1, 1)] },
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 5, 1)] },
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 10, 1)] },
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 12, 1)] },
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 13, 1)] },
		];

		this.VerifyCSharpDiagnostic(test, expected);

		const string fixtest = @"	// On first line
namespace ConsoleApplication1
{
	using System;
	using System.Collections.Generic;

	class TypeName
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public TypeName()
		{
		}
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	#endregion
}