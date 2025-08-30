namespace Menees.Analyzers.Test;

using Microsoft.CodeAnalysis.CodeFixes;

[TestClass]
public class Men018UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men018UseDigitSeparators();

	protected override CodeFixProvider? CSharpCodeFixProvider => new Men018UseDigitSeparatorsFixer();

	protected override IEnumerable<Type> AssemblyRequiredTypes => [typeof(Console)];

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"
public class Test
{
	private const int Million = 1_000_000;
	private const uint MaxUint = 0xFFFF_FFFFu;
	private const decimal TenBillion = 10_000_000_000m;
	private const int Thousand = 1_000;
	private const uint HexHundred = 0x1_00u;
	private const ulong BinaryMillion = 0b100_0000UL;
	private const double BigDouble = 123_456.789;

	private const float BigFloat = 123_456e-3f;
	private const int SmallDecimal = 123;
	private const uint SmallHexadecimal = 0xFFu;
	private const long SmallBinary = 0b0101L;
}";

		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTest

	[TestMethod]
	public void InvalidCodeTest()
	{
		const string test = @"
public class Test
{
	private const int Million = 1000000;
	private const uint MaxUint = 0xFFFFFFFFu;
	private const decimal TenBillion = 10000000000m;
	private const int Thousand = 1000;
	private const uint HexHundred = 0x100u;
	private const ulong BinaryMillion = 0b1000000UL;
	private const double BigDouble = 123456.7890;
}";

		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 1000000 should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 4, 30)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 0xFFFFFFFFu should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 5, 31)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 10000000000m should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 37)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 1000 should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 31)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 0x100u should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 34)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 0b1000000UL should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 9, 38)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 123456.7890 should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 10, 35)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void InvalidCodeFixerTest()
	{
		const string before = @"
using System;
public class Test
{
	private static readonly DateTime SomeDay = new(2024, 5, 28);
	private const int Million = 1000000;
	private const uint MaxUint = 0xFFFFFFFFu;
	private const decimal TenBillion = 10000000000m;
	private const int Thousand = 1000;
	private const uint HexHundred = 0x100u;
	private const ulong BinaryMillion = 0b1000000UL;
	private const double BigDouble = 123456.7890;
	private static readonly double Conditional = Convert.ToBoolean(0) ? 0xFFFFFF : 1234.56789;
	public Test()
	{
		if (DateTime.Now.TimeOfDay.TotalSeconds >= 43200) Console.WriteLine(""Afternoon"");
	}
}";

		const string after = @"
using System;
public class Test
{
	private static readonly DateTime SomeDay = new(2_024, 5, 28);
	private const int Million = 1_000_000;
	private const uint MaxUint = 0x_FF_FF_FF_FFu;
	private const decimal TenBillion = 10_000_000_000m;
	private const int Thousand = 1_000;
	private const uint HexHundred = 0x1_00u;
	private const ulong BinaryMillion = 0b100_0000UL;
	private const double BigDouble = 123_456.789_0;
	private static readonly double Conditional = Convert.ToBoolean(0) ? 0x_FF_FF_FF : 1_234.567_89;
	public Test()
	{
		if (DateTime.Now.TimeOfDay.TotalSeconds >= 43_200) Console.WriteLine(""Afternoon"");
	}
}";

		this.VerifyCSharpFix(before, after);
	}

	#endregion
}