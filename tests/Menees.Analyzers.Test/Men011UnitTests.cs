namespace Menees.Analyzers.Test;

[TestClass]
public sealed class Men011UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override CodeFixProvider CSharpCodeFixProvider => new Men011AlignUsingDirectivesFixer();

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men011AlignUsingDirectives();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string SpaceTab = "    ";
		const string test = @"
using System; // Comment after.
using System.Text;

namespace Test
{
	using System.Xml;
	// Comment in the middle.
	using System.Collections;

	namespace Sub
	{
		// Comment before using directives.
" + SpaceTab + SpaceTab + @"using System.Linq;
" + SpaceTab + SpaceTab + @"using System.Diagnostics;

		class TypeName
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
		const string SpaceTab = "    ";
		const string test = @"
	using System; // Comment after.
using System.Text;

namespace Test
{
	using System.Xml;
	// Comment in the middle.
using System.Collections;
" + SpaceTab + @"using System.Collections.Generic;

	namespace Sub
	{
		// Comment before using directives.
" + SpaceTab + @"using System.Linq;
" + SpaceTab + SpaceTab + @"using System.Diagnostics;

		class TypeName
		{
		}
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 2, 2)] },
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 9, 1)] },
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 15, 5)] },
			new DiagnosticResult(analyzer) { Locations = [new DiagnosticResultLocation("Test0.cs", 16, 9)] },
		];

		this.VerifyCSharpDiagnostic(test, expected);

		// Note: The default workspace options use 4 spaces instead of tabs,
		// so any inconsistent indentations will be fixed with 4 space indents.
		const string fixtest = @"
using System; // Comment after.
using System.Text;

namespace Test
{
	using System.Xml;
	// Comment in the middle.
" + SpaceTab + @"using System.Collections;
" + SpaceTab + @"using System.Collections.Generic;

	namespace Sub
	{
		// Comment before using directives.
" + SpaceTab + SpaceTab + @"using System.Linq;
" + SpaceTab + SpaceTab + @"using System.Diagnostics;

		class TypeName
		{
		}
	}
}";

		this.VerifyCSharpFix(test, fixtest);
	}

	#endregion
}