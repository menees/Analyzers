namespace Menees.Analyzers;

#region Using Directives

using System;
using System.Collections.Generic;
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

	#endregion

	#region Constructors

	private NumericLiteral(string text, NumericBase numericBase, int digitsStartIndex, int digitsLength, bool isInteger)
	{
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

	public static bool TryParse(string? text, out NumericLiteral? value)
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
		out NumericLiteral? value)
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
			else if (text.Length > 1 && IntegerSuffixes.Contains(text[text.Length - 1].ToString()))
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

	private static bool TryParseReal(string text, out NumericLiteral? value)
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

	#endregion
}
