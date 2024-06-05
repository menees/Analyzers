namespace Menees.Analyzers;

#region Using Directives

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

#endregion

internal sealed partial class Settings
{
	#region Private Data Members

	private const string SettingsBaseName = "Menees.Analyzers.Settings";
	private const string SettingsFileName = SettingsBaseName + ".xml";

	private static readonly SourceTextValueProvider<Settings> ValueProvider = new(LoadSettings);

	private static readonly IEnumerable<Predicate<string>> DefaultAnalyzeFileNameExclusions =
	[
		CreateFileNamePredicate("GeneratedCode.cs"),
		CreateFileRegexPredicate(@"\.(designer|generated|codegen)\.cs$"),
	];

	private static readonly IEnumerable<Predicate<string>> DefaultTypeFileNameExclusions =
	[
		CreateFileNamePredicate("Enumerations.cs"),
		CreateFileRegexPredicate(@"^Enumerations\..*\.cs$"),
		CreateFileNamePredicate("Interfaces.cs"),
		CreateFileRegexPredicate(@"^Interfaces\..*\.cs$"),
		CreateFileNamePredicate("Delegates.cs"),
		CreateFileRegexPredicate(@"^Delegates\..*\.cs$"),

		// Razor pages like Page.cshtml.cs contain PageModel instead of just Page.
		CreateFileRegexPredicate(@"^.*\.cshtml.*\.cs$"),

		// Note: We don't need to do the following because these files shouldn't contain types (just assembly attributes):
		// CreateFileRegexPredicate(".*AssemblyInfo\.cs$"),
		// CreateFileNamePredicate("GlobalSuppressions.cs"),
	];

	private static readonly Dictionary<string, string> DefaultPreferredTerms = new()
	{
		// These are the single terms from CA1726 plus the special case of "ID -> Id" from CA1709.
		// https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1726?view=vs-2019#rule-description
		// https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1709?view=vs-2019#rule-description
		{ "Arent", "AreNot" },
		{ "Cancelled", "Canceled" },
		{ "Cant", "Cannot" },
		{ "Couldnt", "CouldNot" },
		{ "Didnt", "DidNot" },
		{ "Doesnt", "DoesNot" },
		{ "Dont", "DoNot" },
		{ "Hadnt", "HadNot" },
		{ "Hasnt", "HasNot" },
		{ "Havent", "HaveNot" },
		{ "ID", "Id" },
		{ "Indices", "Indexes" },
		{ "Isnt", "IsNot" },
		{ "Shouldnt", "ShouldNot" },
		{ "Wasnt", "WasNot" },
		{ "Werent", "WereNot" },
		{ "Wont", "WillNot" },
		{ "Wouldnt", "WouldNot" },
		{ "Writeable", "Writable" },
	};

	private static readonly Settings DefaultSettings = new();

	private readonly IEnumerable<Predicate<string>> analyzeFileNameExclusions;
	private readonly IEnumerable<Predicate<string>> typeFileNameExclusions;
	private readonly HashSet<string> allowedNumericLiterals = new(["0", "1", "2", "100"]);
	private readonly HashSet<string> allowedNumericCallerNames = new(
	[
		"FromDays", "FromHours", "FromMicroseconds", "FromMilliseconds", "FromMinutes", "FromSeconds", "FromTicks", "MaxLength"
	]);
	private readonly IEnumerable<Predicate<string>>? allowedNumericCallerRegexes;
	private readonly Dictionary<string, string> preferredTerms = DefaultPreferredTerms;

	#endregion

	#region Constructors

	private Settings()
	{
		// This just uses the default settings.
		this.IsDefault = true;
		this.analyzeFileNameExclusions = DefaultAnalyzeFileNameExclusions;
		this.typeFileNameExclusions = DefaultTypeFileNameExclusions;
	}

	private Settings(XElement xml)
	{
		this.TabSize = GetSetting(xml, nameof(this.TabSize), this.TabSize);
		this.MaxLineColumns = GetSetting(xml, nameof(this.MaxLineColumns), this.MaxLineColumns);
		this.NotifyLineColumns = GetSetting(xml, nameof(this.NotifyLineColumns), this.NotifyLineColumns);
		this.MaxMethodLines = GetSetting(xml, nameof(this.MaxMethodLines), this.MaxMethodLines);
		this.MaxPropertyAccessorLines = GetSetting(xml, nameof(this.MaxPropertyAccessorLines), this.MaxPropertyAccessorLines);
		this.MaxFileLines = GetSetting(xml, nameof(this.MaxFileLines), this.MaxFileLines);
		this.MaxUnregionedLines = GetSetting(xml, nameof(this.MaxUnregionedLines), this.MaxUnregionedLines);
		this.AllowLongUriLines = GetSetting(xml, nameof(this.AllowLongUriLines), this.AllowLongUriLines);
		this.AllowLongFourSlashCommentLines = GetSetting(xml, nameof(this.AllowLongFourSlashCommentLines), this.AllowLongFourSlashCommentLines);

		this.analyzeFileNameExclusions = GetFileNameExclusions(xml, "AnalyzeFileNameExclusions", DefaultAnalyzeFileNameExclusions);
		this.typeFileNameExclusions = GetFileNameExclusions(xml, "TypeFileNameExclusions", DefaultTypeFileNameExclusions);

		XElement allowedNumericLiteralsElement = xml.Element("AllowedNumericLiterals");
		if (allowedNumericLiteralsElement != null)
		{
			this.allowedNumericLiterals = new HashSet<string>(
				allowedNumericLiteralsElement.Elements("Literal").Select(literal => literal.Value));
			this.allowedNumericCallerNames = new HashSet<string>(
				allowedNumericLiteralsElement.Elements("CallerName").Select(callerName => callerName.Value));
			this.allowedNumericCallerRegexes = allowedNumericLiteralsElement.Elements("CallerRegex")
				.Select<XElement, Predicate<string>>(callerRegex => callerName => Regex.IsMatch(callerName, callerRegex.Value)).ToList();
		}

		XElement unitTestAttributes = xml.Element("UnitTestAttributes");
		if (unitTestAttributes != null)
		{
			this.TestClassAttributeNames = new HashSet<string>(
				unitTestAttributes.Elements("Class").Select(element => element.Value));

			this.TestMethodAttributeNames = new HashSet<string>(
				unitTestAttributes.Elements("Method").Select(element => element.Value));
		}

		XElement preferredTermsElement = xml.Element("PreferredTerms");
		if (preferredTermsElement != null)
		{
			this.preferredTerms = preferredTermsElement.Elements("Term")
				.Select(term => new KeyValuePair<string, string>(term.Attribute("Avoid").Value, term.Attribute("Prefer").Value))
				.ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		XElement digitSeparators = xml.Element("DigitSeparators");
		if (digitSeparators != null)
		{
			this.DecimalSeparators = GetDigitSeparatorFormat(digitSeparators.Element("Decimal"), this.DecimalSeparators);
			this.HexadecimalSeparators = GetDigitSeparatorFormat(digitSeparators.Element("Hexadecimal"), this.HexadecimalSeparators);
			this.BinarySeparators = GetDigitSeparatorFormat(digitSeparators.Element("Binary"), this.BinarySeparators);
		}
	}

	#endregion

	#region Public Properties

	public static Settings Default => DefaultSettings;

	public bool IsDefault { get; }

	/// <summary>
	/// Gets the tab size to use when applying diagnostic analyzers,
	/// which don't have access to the user's workspace options.
	///
	/// This should NOT be used by code fix providers because they
	/// can get the user's preferred tab size from the workspace options
	/// (e.g., Men001CodeFixProvider.GetTabSize).
	/// </summary>
	public int TabSize { get; } = 4;

	// NOTE: NotifyLineColumns uses the same default value as MaxLineColumns so Men002LineTooLong
	// can skip all Notify checking unless MEN002A is enabled AND NotifyLineColumns is explicitly
	// configured to be less than MaxLineColumns.
	public int NotifyLineColumns { get; } = 160;

	public int MaxLineColumns { get; } = 160;

	public int MaxMethodLines { get; } = 120;

	public int MaxPropertyAccessorLines { get; } = 80;

	public int MaxFileLines { get; } = 2000;

	public int MaxUnregionedLines { get; } = 100;

	// These attributes cover MSTest, NUnit, and xUnit.
	public HashSet<string> TestMethodAttributeNames { get; } = new HashSet<string>(["TestMethod", "Test", "Fact"]);

	public HashSet<string> TestClassAttributeNames { get; } = new HashSet<string>(["TestClass", "TestFixture"]);

	public bool HasPreferredTerms => this.preferredTerms.Count > 0;

	public bool AllowLongUriLines { get; } = true;

	public bool AllowLongFourSlashCommentLines { get; }

	#endregion

	#region Private Properties

	private (byte MinSize, byte GroupSize) DecimalSeparators { get; } = (6, 3); // Group Per-Thousand

	private (byte MinSize, byte GroupSize) HexadecimalSeparators { get; } = (8, 4); // Group Per-Word

	private (byte MinSize, byte GroupSize) BinarySeparators { get; } = (8, 4); // Group Per-Nibble

	#endregion

	#region Public Methods

	public static Settings Cache(AnalysisContext context, AnalyzerOptions options, CancellationToken cancellationToken)
	{
		// See docs for using AdditionalFiles:
		// https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Using%20Additional%20Files.md
		// Using OrdinalIgnoreCase to compare file names per MSDN:
		// https://msdn.microsoft.com/en-us/library/dd465121.aspx#choosing_a_stringcomparison_member_for_your_method_call
		AdditionalText? additionalText = options?.AdditionalFiles
			.FirstOrDefault(file => string.Equals(Path.GetFileName(file.Path), SettingsFileName, StringComparison.OrdinalIgnoreCase));

		SourceText? sourceText = additionalText?.GetText(cancellationToken);
		if (sourceText == null || !context.TryGetValue(sourceText, ValueProvider, out Settings? result))
		{
			result = DefaultSettings;
		}

		return result;
	}

	public static bool TryParseIntegerLiteral(string text, out ulong value)
	{
		(string scrubbed, NumericBase numericBase) = SplitNumericLiteral(text);
		bool result;
		switch (numericBase)
		{
			case NumericBase.Hexadecimal:
				result = ulong.TryParse(scrubbed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
				break;
			case NumericBase.Binary:
				try
				{
					// They began the literal with a "0b" prefix, so this should always succeed unless
					// they've typed an invalid binary literal (e.g., too long or with non-0|1 chars.
					value = Convert.ToUInt64(scrubbed, 2);
					result = true;
				}
				catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
				{
					value = 0;
					result = false;
				}

				break;
			default:
				result = ulong.TryParse(scrubbed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
				break;
		}

		return result;
	}

	public static bool TryParseIntegerLiteral(string text, out byte value)
	{
		bool result = TryParseIntegerLiteral(text, out ulong bigValue)
			&& bigValue >= byte.MinValue && bigValue <= byte.MaxValue;
		value = result ? (byte)bigValue : (byte)0;
		return result;
	}

	public bool IsAnalyzeFileNameCandidate(string filePath)
		=> IsFileNameCandidate(filePath, this.analyzeFileNameExclusions);

	public bool IsTypeFileNameCandidate(string filePath)
		=> IsFileNameCandidate(filePath, this.typeFileNameExclusions);

	public bool IsAllowedNumericLiteral(string text)
	{
		bool result = this.allowedNumericLiterals.Contains(text);
		if (!result)
		{
			// Ignore any type suffix (e.g., m, L), base prefix (e.g., 0x or 0b), leading zeros,
			// or trailing fractional zeros (e.g., .0 on 1.0).
			(string scrubbed, _) = SplitNumericLiteral(text);
			result = this.allowedNumericLiterals.Contains(scrubbed);
		}

		return result;
	}

	public bool IsAllowedNumericLiteralCaller(string callerName)
	{
		bool result = this.allowedNumericCallerNames.Contains(callerName);

		if (!result && this.allowedNumericCallerRegexes != null)
		{
			result = this.allowedNumericCallerRegexes.Any(regexIsMatch => regexIsMatch(callerName));
		}

		return result;
	}

	public bool UsePreferredTerm(string term, out string preferredTerm)
	{
		bool result = false;

		if (string.IsNullOrEmpty(term))
		{
			preferredTerm = term;
		}
		else if (char.IsLower(term[0]))
		{
			StringBuilder change = new(term);
			change[0] = char.ToUpper(change[0]);
			term = change.ToString();
			if (this.preferredTerms.TryGetValue(term, out preferredTerm))
			{
				change = new(preferredTerm);
				change[0] = char.ToLower(change[0]);
				preferredTerm = change.ToString();
				result = true;
			}
		}
		else if (this.preferredTerms.TryGetValue(term, out preferredTerm))
		{
			result = true;
		}

		return result;
	}

	public (byte MinSize, byte GroupSize) GetDigitSeparatorFormat(NumericLiteral literal)
	{
		(byte MinSize, byte GroupSize) result = literal.Base switch
		{
			NumericBase.Hexadecimal => this.HexadecimalSeparators,
			NumericBase.Binary => this.BinarySeparators,
			_ => this.DecimalSeparators,
		};

		return result;
	}

	#endregion

	#region Private Methods

	private static Settings LoadSettings(SourceText sourceText)
	{
		string text = sourceText.ToString();
		XElement xml = XElement.Parse(text);
		Settings result = new(xml);
		return result;
	}

	private static int GetSetting(XElement xml, string elementName, int defaultValue)
		=> GetSetting(xml, elementName, defaultValue, (string text, out int value) => int.TryParse(text, out value) && value > 0);

	private static bool GetSetting(XElement xml, string elementName, bool defaultValue)
		=> GetSetting(xml, elementName, defaultValue, bool.TryParse);

	private static T GetSetting<T>(XElement xml, string elementName, T defaultValue, TryParse<T> tryParse)
	{
		T result = defaultValue;

		XElement element = xml.Element(elementName);
		if (element != null)
		{
			if (tryParse(element.Value, out T value))
			{
				result = value;
			}
		}

		return result;
	}

	private static Predicate<string> CreateFileRegexPredicate(string fileRegex)
		=> value => Regex.IsMatch(value, fileRegex, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

	private static Predicate<string> CreateFileNamePredicate(string fileName)
		=> value => string.Equals(value, fileName, StringComparison.CurrentCultureIgnoreCase);


	private static IEnumerable<Predicate<string>> GetFileNameExclusions(
		XElement xml, string elementName, IEnumerable<Predicate<string>> defaultExclusions)
	{
		IEnumerable<Predicate<string>> result = defaultExclusions;

		XElement exclusionsElement = xml.Element(elementName);
		if (exclusionsElement != null)
		{
			result =
				exclusionsElement.Elements("FileRegex").Select(element => CreateFileRegexPredicate(element.Value))
				.Concat(exclusionsElement.Elements("FileName").Select(element => CreateFileNamePredicate(element.Value)))
				.ToList();
		}

		return result;
	}

	private static bool IsFileNameCandidate(string filePath, IEnumerable<Predicate<string>> exclusions)
	{
		bool result = false;

		if (!string.IsNullOrEmpty(filePath))
		{
			string fileName = Path.GetFileName(filePath);
			result = !exclusions.Any(isExcluded => isExcluded(fileName));
		}

		return result;
	}

	private static (string Scrubbed, NumericBase Base) SplitNumericLiteral(string text)
	{
		NumericBase numericBase = NumericBase.Decimal;
		if (NumericLiteral.TryParse(text, out NumericLiteral? literal))
		{
			numericBase = literal.Base;

			// Remove digit separators first so we can shrink the text correctly (e.g., trim leading zeros).
			text = literal.ScrubbedDigits;
			int substringLength = text.Length;

			// Skip a fractional zero, which can be used to make a double without using a suffix.
			const string FractionalZero = ".0";
			if (substringLength > FractionalZero.Length
				&& string.CompareOrdinal(text, substringLength - FractionalZero.Length, FractionalZero, 0, FractionalZero.Length) == 0)
			{
				substringLength -= FractionalZero.Length;
			}

			// Skip leading zeros but not the final zero.  This allows 00 to match 0, for example.
			int startIndex = 0;
			while (startIndex < text.Length && substringLength > 1 && text[startIndex] == '0')
			{
				startIndex++;
				substringLength--;
			}

			if ((startIndex + substringLength) <= text.Length && (startIndex > 0 || substringLength < text.Length))
			{
				text = text.Substring(startIndex, substringLength);
			}
		}

		return (text, numericBase);
	}

	private (byte MinSize, byte GroupSize) GetDigitSeparatorFormat(XElement? baseElement, (byte MinSize, byte GroupSize) defaultSeparators)
	{
		(byte MinSize, byte GroupSize) result = defaultSeparators;

		if (baseElement != null)
		{
			byte minSize = GetByte(baseElement, "MinSize", defaultSeparators.MinSize);
			byte groupSize = GetByte(baseElement, "GroupSize", defaultSeparators.GroupSize);
			result = (minSize, groupSize);
		}

		static byte GetByte(XElement element, string attributeName, byte defaultValue)
		{
			string? value = element.Attribute(attributeName)?.Value;
			if (!byte.TryParse(value, out byte result))
			{
				result = defaultValue;
			}

			return result;
		}

		return result;
	}

	#endregion

	#region Private Delegates

	private delegate bool TryParse<T>(string text, out T value);

	#endregion
}
