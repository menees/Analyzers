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

	[TestMethod]
	public void ToStringTest()
	{
		Test("123", "123");
		Test("10543765Lu", "10_543_765Lu");
		Test("1_2__3___4____5", "12_345");

		Test("0xFf", "0x_Ff");
		Test("0X1ba044fEL", "0X_1b_a0_44_fEL");
		Test("0x1ade_3FE1_29AaUL", "0x_1a_de_3F_E1_29_AaUL");
		Test("0x1ade_3FE1_29AaUL", "0x_1ade_3FE1_29AaUL", 4);
		Test("0x_abc", "0xa_bc");
		Test("0x123d", "0x_12_3d");
		Test("0x123d", "0x_1_2_3_d", 1);

		Test("0b101", "0b101");
		Test("0B1001_1010u", "0B_1001_1010u");
		Test("0b1111_1111_0000UL", "0b_1111_1111_0000UL");
		Test("0B__111", "0B111");

		Test("1.234_567", "1.234567", 0);
		Test("1_234.567", "1234.567", 0);

		Test("1.234567", "1.234_567");
		Test("123.4567", "123.456_7");
		Test("1234.567", "1_234.567");
		Test(".123456", ".123_456");
		Test(".1234567", ".123_456_7");
		Test(".12345e67", ".123_45e67");
		Test("1234567d", "1_234_567d");
		Test("12345e67", "12_345e67");
		Test("1234.567e89", "1_234.567e89");
		Test("1234.5678e9", "1_234.567_8e9");

		Test(".3e5f", ".3e5f");
		Test("2345E-2_0", "2_345E-20");
		Test("15D", "15D");
		Test("19.73M", "19.73M");
		Test("1234d", "1_234d");

		static void Test(string text, string expected, byte? groupSize = null)
		{
			NumericLiteral.TryParse(text, out NumericLiteral? literal).ShouldBeTrue(text);
			literal.ToString().ShouldBe(text);
			literal.ToString(0).ShouldBe(text.Replace("_", string.Empty));

			groupSize ??= literal.Base switch
			{
				NumericBase.Hexadecimal => 2,
				NumericBase.Binary => 4,
				_ => 3,
			};

			literal.ToString(groupSize.Value).ShouldBe(expected, text);
		}
	}

	[TestMethod]
	public void GetSizeTest()
	{
		Test("123", 3);
		Test("10543765Lu", 8);
		Test("1_2__3___4____5", 5);

		Test("0xFf", 2);
		Test("0X1ba044fEL", 8);

		Test("0B1001_1010u", 8);
		Test("0b1111_1111_0000UL", 12);
		Test("0B__111", 3);

		Test("1.234_567", 6);
		Test("1_234.567", 4);
		Test("123_456.7", 6);
		Test(".123456", 6);
		Test(".12345e67", 5);
		Test("1234567d", 7);
		Test("1234.567e89", 4);

		Test(".3e5f", 1);
		Test("2345E-2_0", 4);
		Test("15D", 2);
		Test("19.73M", 2);

		static void Test(string text, byte  expected)
		{
			NumericLiteral.TryParse(text, out NumericLiteral? literal).ShouldBeTrue(text);
			literal.GetSize().ShouldBe(expected, text);
		}
	}
}