namespace Menees.Analyzers;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#endregion

public sealed class NumericLiteral
{
	#region Private Data Members

	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#6453-integer-literals
	private static readonly HashSet<string> IntegerSuffixes = ["U", "u", "L", "l", "UL", "Ul", "uL", "ul", "LU", "Lu", "lU", "lu"];

	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#6454-real-literals
	private static readonly HashSet<char> RealSuffixes = ['F', 'f', 'D', 'd', 'M', 'm'];

	private static readonly HashSet<char> BinaryDigits = ['0', '1'];
	private static readonly HashSet<char> DecimalDigits = [.. BinaryDigits, '2', '3', '4', '5', '6', '7', '8', '9'];
	private static readonly HashSet<char> HexadecimalDigits = [.. DecimalDigits, 'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f'];
	private static readonly char[] ExponentChar = ['e', 'E'];

	private string originalText;

	#endregion

	#region Constructors

	private NumericLiteral(
		string text,
		NumericBase numericBase,
		int digitsStartIndex,
		int digitsLength,
		bool isInteger)
	{
		this.originalText = text;
		this.Base = numericBase;
		this.Prefix = text.Substring(0, digitsStartIndex);
		this.OriginalDigits = text.Substring(digitsStartIndex, digitsLength);
		this.ScrubbedDigits = Scrub(this.OriginalDigits);
		this.Suffix = text.Substring(digitsStartIndex + digitsLength);
		this.IsInteger = isInteger;
	}

	#endregion

	#region Public Properties

	/// <summary>
	/// Gets whether the literal is base 10, base 16, or base 2.
	/// </summary>
	public NumericBase Base { get; }

	/// <summary>
	/// Gets the prefix (e.g., "0x", "0b") if present or an empty string.
	/// </summary>
	public string Prefix { get; }

	/// <summary>
	/// Gets the original "digits" from the numeric literal including any '_' separators.
	/// If this is a real literal, this may also contain exponent and sign characters.
	/// </summary>
	public string OriginalDigits { get; }

	/// <summary>
	/// Gets the scrubbed "digits" from the numeric literal with all '_' separators removed.
	/// If this is a real literal, this may also contain exponent and sign characters.
	/// </summary>
	public string ScrubbedDigits { get; }

	/// <summary>
	/// Gets the suffix (e.g., "L", "UL") if present or an empty string.
	/// </summary>
	public string Suffix { get; }

	/// <summary>
	/// Gets whether this is an integer literal (e.g., 123u) and not a real literal (e.g., 123d).
	/// </summary>
	public bool IsInteger { get; }

	#endregion

	#region Public Methods

	public static bool TryParse(string? text, [NotNullWhen(true)] out NumericLiteral? value)
	{
		text ??= string.Empty;
		text = text.Trim();
		value = null;

		if (TryParseInteger(text, string.Empty, NumericBase.Decimal, DecimalDigits, out NumericLiteral? literal)
			|| TryParseInteger(text, "0x", NumericBase.Hexadecimal, HexadecimalDigits, out literal)
			|| TryParseInteger(text, "0b", NumericBase.Binary, BinaryDigits, out literal))
		{
			value = literal;
		}
		else if (TryParseReal(text, out literal))
		{
			value = literal;
		}

		return value != null;
	}

	public override string ToString() => this.originalText;

	public string ToString(byte groupSize)
	{
		string result;

		if (groupSize == 0)
		{
			result = $"{this.Prefix}{this.ScrubbedDigits}{this.Suffix}";
		}
		else
		{
			const int SeparatorPadding = 10;
			StringBuilder sb = new(this.Prefix.Length + this.ScrubbedDigits.Length + this.Suffix.Length + SeparatorPadding);
			sb.Append(this.Prefix);

			if (this.IsInteger)
			{
				this.AppendIntegerDigits(sb, 0, this.ScrubbedDigits.Length, groupSize, this.Prefix.Length > 0);
			}
			else
			{
				// A real number is basically "Integer Fraction Exponent", and any of those parts can be empty.
				// They can't all be empty, and we can't have just an Exponent part. The other six possibilities
				// are allowed: .123, .12e3, 123d, 12e3, 1.23, 1.2e3
				int decimalIndex = this.ScrubbedDigits.IndexOf('.');
				int exponentIndex = this.ScrubbedDigits.IndexOfAny(ExponentChar, decimalIndex + 1);

				if (decimalIndex < 0 && exponentIndex < 0)
				{
					// Integer part only. Example: 123d
					this.AppendIntegerDigits(sb, 0, this.ScrubbedDigits.Length, groupSize);
				}
				else if (exponentIndex < 0)
				{
					// Has fraction part and may have an integer part. Examples: .123 or 1.23
					this.AppendIntegerDigits(sb, 0, decimalIndex, groupSize);
					sb.Append(this.ScrubbedDigits[decimalIndex]);
					int fractionIndex = decimalIndex + 1;
					this.AppendScrubbedDigits(sb, fractionIndex, this.ScrubbedDigits.Length - fractionIndex, groupSize, fractionIndex + groupSize);
				}
				else if (decimalIndex < 0)
				{
					// Has exponent part with a required integer part. Example: 12e3
					this.AppendIntegerDigits(sb, 0, exponentIndex, groupSize);

					// See comments below about why we don't format the exponent.
					sb.Append(this.ScrubbedDigits, exponentIndex, this.ScrubbedDigits.Length - exponentIndex);
				}
				else
				{
					// Has fraction part, exponent part, and maybe an integer part. Examples: .12e3 or 1.2e3
					this.AppendIntegerDigits(sb, 0, decimalIndex, groupSize);
					sb.Append(this.ScrubbedDigits[decimalIndex]);
					int fractionIndex = decimalIndex + 1;
					this.AppendScrubbedDigits(sb, fractionIndex, exponentIndex - fractionIndex, groupSize, fractionIndex + groupSize);

					// Note: Double only allows 3 digit exponents, so we'll never try to format those.
					// Technically, we could if groupSize < 3, but it's so rare that it's not worth the
					// hassle of parsing the optional sign and doing another integer right-to-left format.
					sb.Append(this.ScrubbedDigits, exponentIndex, this.ScrubbedDigits.Length - exponentIndex);
				}
			}

			sb.Append(this.Suffix);
			result = sb.ToString();
		}

		return result;
	}

	#endregion

	#region Private Methods

	private static string Scrub(string text) => text.Replace("_", string.Empty);

	private static bool IsPotentiallyValid(string text, int digitsStartIndex, int digitsLength)
	{
		bool result = digitsLength > 0
			&& text[0] != '_' // _123 is invalid but 0x_123 is valid.
			&& text[digitsStartIndex + digitsLength - 1] != '_'; // Separators can never be last.
		return result;
	}

	private static bool TryParseInteger(
		string text,
		string prefix,
		NumericBase numericBase,
		HashSet<char> allowedDigits,
		[NotNullWhen(true)] out NumericLiteral? value)
	{
		value = null;

		if (prefix.Length == 0 || text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			// Try to match the longest suffixes first.
			int suffixLength = 0;
			if (text.Length > 2 && IntegerSuffixes.Contains(text.Substring(text.Length - 2)))
			{
				suffixLength = 2;
			}
			else if (text.Length > 1 && IntegerSuffixes.Contains(text[^1].ToString()))
			{
				suffixLength = 1;
			}

			int digitsStartIndex = prefix.Length;
			int digitsLength = text.Length - digitsStartIndex - suffixLength;
			if (IsPotentiallyValid(text, digitsStartIndex, digitsLength)
				&& text.Skip(digitsStartIndex).Take(digitsLength).All(ch => allowedDigits.Contains(ch) || ch == '_'))
			{
				value = new(text, numericBase, digitsStartIndex, digitsLength, true);
			}
		}

		return value != null;
	}

	private static bool TryParseReal(string text, [NotNullWhen(true)] out NumericLiteral? value)
	{
		value = null;

		int digitsLength = text.Length;
		char suffix = text[digitsLength - 1];
		if (RealSuffixes.Contains(suffix))
		{
			digitsLength--;
		}

		if (IsPotentiallyValid(text, 0, digitsLength)
			&& !text.Contains("_.")
			&& !text.Contains("._")
			&& !text.Contains("E_")
			&& !text.Contains("e_")
			&& !(digitsLength < text.Length && text[digitsLength - 1] == '.'))
		{
			string digits = Scrub(text.Substring(0, digitsLength));
			bool canParseDigits = suffix switch
			{
				'M' or 'm' => decimal.TryParse(digits, out _),
				'F' or 'f' => float.TryParse(digits, out _),
				_ => double.TryParse(digits, out _),
			};

			if (canParseDigits)
			{
				value = new(text, NumericBase.Decimal, 0, digitsLength, false);
			}
		}

		return value != null;
	}

	/// <summary>
	/// Appends the integer portion of <see cref="ScrubbedDigits"/> with separators added from right to left.
	/// </summary>
	private void AppendIntegerDigits(
		StringBuilder sb,
		int startIndex,
		int length,
		byte groupSize,
		bool allowLeadingSeparator = false)
	{
		// A separator can't come first, but it can follow a prefix. A separator can never be last.
		int modulus = length % groupSize;
		int separatorIndex = (modulus == 0 && !allowLeadingSeparator ? groupSize : modulus) + startIndex;
		AppendScrubbedDigits(sb, startIndex, length, groupSize, separatorIndex);
	}

	/// <summary>
	/// Appends any portion of <see cref="ScrubbedDigits"/> with separators added from left to right.
	/// </summary>
	private void AppendScrubbedDigits(
		StringBuilder sb,
		int startIndex,
		int length,
		byte groupSize,
		int separatorIndex)
	{
		for (int index = startIndex; index < (startIndex + length); index++)
		{
			if (index == separatorIndex)
			{
				separatorIndex += groupSize;
				if (sb.Length > 0)
				{
					sb.Append('_');
				}
			}

			sb.Append(this.ScrubbedDigits, index, 1);
		}
	}

	#endregion
}
