namespace Menees.Analyzers.Test;

[TestClass]
public class NumericLiteralTests
{
	// Note: Many of these test cases come from the C# language reference.
	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#6453-integer-literals
	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#6454-real-literals

	[TestMethod]
	public void TryParseValidTest()
	{
		Test("123"); // decimal, int
		Test("10_543_765Lu", "10_543_765", suffix: "Lu"); // decimal, ulong
		Test("1_2__3___4____5"); // decimal, int

		Test("0xFf", "Ff", "0x", numericBase: NumericBase.Hexadecimal); // hex, int
		Test("0X1b_a0_44_fEL", "1b_a0_44_fE", "0X", "L", numericBase: NumericBase.Hexadecimal); // hex, long
		Test("0x1ade_3FE1_29AaUL", "1ade_3FE1_29Aa", "0x", "UL", numericBase: NumericBase.Hexadecimal); // hex, ulong
		Test("0x_abc", "_abc", "0x", numericBase: NumericBase.Hexadecimal); // hex, int
		Test("0x123d", "123d", "0x", numericBase: NumericBase.Hexadecimal); // 0x prefix must be parsed before 'd' "suffix".

		Test("0b101", "101", "0b", numericBase: NumericBase.Binary); // binary, int
		Test("0B1001_1010u", "1001_1010", "0B", "u", numericBase: NumericBase.Binary); // binary, uint
		Test("0b1111_1111_0000UL", "1111_1111_0000", "0b", "UL", numericBase: NumericBase.Binary); // binary, ulong
		Test("0B__111", "__111", "0B", numericBase: NumericBase.Binary); // binary, int

		Test("1.234_567"); // double
		Test(".3e5f", ".3e5", suffix: "f"); // float
		Test("2_345E-2_0"); // double
		Test("15D", "15", suffix: "D"); // double
		Test("19.73M", "19.73", suffix: "M"); // decimal
		Test("123d", "123", suffix: "d"); // No prefix, but the suffix says double.

		static void Test(
			string text,
			string? originalDigits = null,
			string? prefix = null,
			string? suffix = null,
			NumericBase numericBase = NumericBase.Decimal)
		{
			NumericLiteral.TryParse(text, out NumericLiteral? literal).ShouldBeTrue(text);
			literal.Base.ShouldBe(numericBase, text);
			literal.OriginalDigits.ShouldBe(originalDigits ?? text, text);
			literal.Prefix.ShouldBe(prefix ?? string.Empty, text);
			literal.Suffix.ShouldBe(suffix ?? string.Empty, text);
			literal.ScrubbedDigits.ShouldBe(literal.OriginalDigits.Replace("_", string.Empty), text);

			HashSet<string> realSuffixes = new(["D", "F", "M"], StringComparer.OrdinalIgnoreCase);
			bool isInteger = !realSuffixes.Contains(literal.Suffix)
				&& (literal.Base == NumericBase.Binary
					|| literal.Base == NumericBase.Hexadecimal
					|| ulong.TryParse(literal.ScrubbedDigits, out _));
			literal.IsInteger.ShouldBe(isInteger, text);
		}
	}

	[TestMethod]
	public void TryParseInvalidTest()
	{
		Test("_123"); // not a numeric literal; identifier due to leading _
		Test("123_"); // invalid; no trailing _allowed
		Test("_0x123"); // not a numeric literal; identifier due to leading _
		Test("0xabc_"); // invalid; no trailing _ allowed
		Test("__0B111"); // not a numeric literal; identifier due to leading _
		Test("0B111__"); // invalid; no trailing _ allowed
		Test("1.F"); // parsed as a member access of F due to non-digit after .
		Test("1_.2F"); // invalid; no trailing _ allowed in integer part
		Test("1._234"); // parsed as a member access of _234 due to non-digit after .
		Test("1.234_"); // invalid; no trailing _ allowed in fraction
		Test(".3e_5F"); // invalid; no leading _ allowed in exponent
		Test(".3e5_F"); // invalid; no trailing _ allowed in exponent

		static void Test(string text)
		{
			NumericLiteral.TryParse(text, out _).ShouldBeFalse(text);
		}
	}
}