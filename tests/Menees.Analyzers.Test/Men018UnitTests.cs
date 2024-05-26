namespace Menees.Analyzers.Test;

[TestClass]
public class Men018UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men018UseDigitSeparators();

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
	private const double BigDouble = 123456.789;
}";

		var analyzer = this.CSharpDiagnosticAnalyzer;
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
				Message = "The numeric literal 123456.789 should use digit separators.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 10, 35)]
			},
		];

		// TODO: Add code fixer unit tests. [Bill, 5/26/2024]
		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}