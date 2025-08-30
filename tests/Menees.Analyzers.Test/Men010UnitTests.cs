namespace Menees.Analyzers.Test;

[TestClass]
public sealed class Men010UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men010AvoidMagicNumbers();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"
using System;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
class MagicNumbers
{
	private enum Test
	{
		Value16 = 0b0001_0000,
		Value101 = 101,
		Value102 = 102,
	}

	private const int WednesdayValue = 11 + 0x00 + 000 + 00_01;
	private static readonly double FridayValue = 19.5;
	private static readonly DateTime SpecialDay = new DateTime(2015, 8, 28);

	private int SundayValue = 7;
	private double MondayValue = 3.0;
	private decimal TuesdayValue = 5.0m;

	public int MaxRate { get; } = 100;

	private static int GetTwice(int value) => 2 * value;

	public TimeSpan GetTime() => TimeSpan.FromDays(3) + TimeSpan.FromHours(4.5) + TimeSpan.FromMinutes(6);

	public double GetRate()
	{
		const double ThursdayValue = 17.5;

		string[] dayNames = new string[SundayValue];
		dayNames[6] = ""Sunday"";
		var list = dayNames.ToList();
		list[5] = dayNames[4];

		switch (DateTime.Today.DayOfWeek)
		{
			case DayOfWeek.Sunday:
				return SundayValue;

			case DayOfWeek.Monday:
				return MondayValue;

			case DayOfWeek.Tuesday:
				return (double)TuesdayValue;

			case DayOfWeek.Wednesday:
				return WednesdayValue;

			case DayOfWeek.Thursday:
				return ThursdayValue;

			case DayOfWeek.Friday:
				return FridayValue;
		}

		DataColumn col = new(""Test"", typeof(string));
		col.MaxLength = 20;

		const int DefaultValue = 17;
		return DefaultValue;
	}

	public static string UseGetIndexes(IDataRecord record)
	{
		DateTime zero = record.GetDateTime(0);
		string one = string.Concat(record.GetString(1), record.GetString(2));
		record.GetBoolean(3);
		record.GetInt64(4);
		record.GetInt32(5);
		if (Convert.ToBoolean(zero > DateTime.MinValue))
		{
			record.GetDouble(GetTwice(0b11));
			record.GetInt16(72);
			record.GetByte(87);
		}

		string x = ""Testing is fun"";
#if NET
		x = x[3..5] + x[6..^3];
#endif
		x.ToString();

		return one;
	}

	[TestMethod]
	public int SpecialRate()
	{
		return 21;
	}
}

[TestClass]
class Tester
{
	public int Test()
	{
		return 123;
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
using System;
class MagicNumbers
{
	private const int WednesdayValue = 11;
	private static ulong SaturdayValue = 11UL; // Not readonly still a simple field init.
	private static ulong AltSaturdayValue = 3UL + SaturdayValue;
	private int sundayValue = 7;
	private static double mondayValue = 3.0;
	private decimal tuesdayValue = 5.0m;
	private float fridayValue = 5.0f + (float)mondayValue;

	public int MaxRate { get; } = 10 * WednesdayValue;

	public double GetRate()
	{
		const double ThursdayValue = 17.5;

		switch (DateTime.Today.DayOfWeek)
		{
			case DayOfWeek.Sunday:
				return sundayValue;

			case DayOfWeek.Monday:
				return mondayValue;

			case DayOfWeek.Tuesday:
				return (double)tuesdayValue;

			case DayOfWeek.Wednesday:
				return WednesdayValue;

			case DayOfWeek.Thursday:
				return ThursdayValue;

			case DayOfWeek.Friday:
				return fridayValue;

			case DayOfWeek.Saturday:
				return SaturdayValue;
		}

		int[] dayCodes = new int[sundayValue];
		var indexTooLarge = dayCodes[256]; // Only 0-255 are allowed non-magic indexes.
		return 17 + dayCodes[sundayValue - 7] + dayCodes[(int)mondayValue + 0b010] + (int)TimeSpan.FromSeconds(5).Ticks;
	}
}";

		var analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 3UL should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 42)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 5.0f should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 11, 30)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 10 should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 13, 32)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 256 should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 44, 32)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 17 should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 45, 10)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 7 should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 45, 38)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 0b010 should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 45, 71)]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "The numeric literal 5 should be replaced with a named constant.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 45, 106)]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}