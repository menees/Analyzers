namespace Menees.Analyzers.Test;

[TestClass]
public class Men012UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men012FlagsShouldBePowersOfTwo();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"using System;
[Flags]
public enum Test : ulong
{
	#region Trivia

	None = 0,
	First = 1u,
	Second = 2L,
	Fourth = +4ul,
	Fifth = (+(8)),
	Sixth = Second | Fourth

	#endregion
}

// This doesn't use [Flags], so it can do whatever it wants.
public enum Test2 : short
{
	None = 0,
	First = (1),
	Second = -2,
	Third = 3,
	Fourth = 19 - 0b11_11
}

[Flags]
internal enum ErrorModes : uint // Base as uint since SetErrorMode takes a UINT.
{
	SYSTEM_DEFAULT = 0x0,
	SEM_FAILCRITICALERRORS = 0x0001,
	SEM_NOALIGNMENTFAULTEXCEPT = 0b0000_0000_0000_0100, // 0x0004,
	SEM_NOGPFAULTERRORBOX = 0x0002,
	SEM_NOOPENFILEERRORBOX = 0x8000,
}

[Flags]
public enum Shifts
{
	None = 0,
	One = 1 << 0,
	Two = 1 << 1,
	Four = 1 << 2,
	Eight = 1 << 3,
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTest

	[TestMethod]
	public void InvalidCodeTest()
	{
		const string test = @"using System;
[Flags]
public enum Test
{
	None = 0,
	First,
	Second = -2,
	Twelve = 12,
	Eight = 3 + 5,
	Seventeen = 0x0011,
}

[Flags]
public enum Unspecified
{
	None,
	First,
	Second
}

[Flags]
public enum Shifts
{
	None = 0,
	One = 1 << 0,
	Two = 1 << -31,
	Four = 1 << 2,
	Eight = 2 << 2,
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Test.First should explicitly assign its value to zero or a power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Test.Second has value -2, which is not a literal power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Test.Twelve has value 12, which is not a power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Test.Eight has value 3 + 5, which is not a literal power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 9, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Test.Seventeen has value 0x0011, which is not a power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 10, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Unspecified.None should explicitly assign its value to zero or a power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 16, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Unspecified.First should explicitly assign its value to zero or a power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 17, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Unspecified.Second should explicitly assign its value to zero or a power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 18, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Shifts.Two has value 1 << -31, which is not a \"1 << x (x >= 0)\" power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 26, 2)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Flags enum member Shifts.Eight has value 2 << 2, which is not a \"1 << x (x >= 0)\" power of two.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 28, 2)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}