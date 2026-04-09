namespace Menees.Analyzers;

#region Using Directives

using System.Xml.Linq;

#endregion

internal sealed class VarStyleCategorySettings
{
	#region Internal Constants

	internal const int DefaultLongTypeNameLength = 30;

	#endregion

	#region Internal Static Fields

	internal static readonly VarStyleCategorySettings None = new();

	#endregion

	#region Constructors

	private VarStyleCategorySettings()
	{
		this.Mode = VarStyleMode.None;
	}

	private VarStyleCategorySettings(
		VarStyleMode mode,
		bool hasConditions,
		bool conditionalForeach,
		bool conditionalLinqScalarResult,
		bool conditionalLinqCollectionResult,
		bool conditionalLinqAggregateResult,
		bool conditionalLongTypeName,
		int longTypeNameLength,
		bool conditionalEvident)
	{
		this.Mode = mode;
		this.HasConditions = hasConditions;
		this.Foreach = conditionalForeach;
		this.LinqScalarResult = conditionalLinqScalarResult;
		this.LinqCollectionResult = conditionalLinqCollectionResult;
		this.LinqAggregateResult = conditionalLinqAggregateResult;
		this.LongTypeName = conditionalLongTypeName;
		this.LongTypeNameLength = longTypeNameLength;
		this.Evident = conditionalEvident;
	}

	#endregion

	#region Public Properties

	public VarStyleMode Mode { get; }

	public bool HasConditions { get; }

	public bool Foreach { get; }

	public bool LinqScalarResult { get; }

	public bool LinqCollectionResult { get; }

	public bool LinqAggregateResult { get; }

	public bool LongTypeName { get; }

	public int LongTypeNameLength { get; }

	public bool Evident { get; }

	#endregion

	#region Internal Methods

	internal static VarStyleCategorySettings Parse(XElement categoryElement)
	{
		VarStyleCategorySettings result;

		XElement? useExplicitType = categoryElement.Element("UseExplicitType");
		if (useExplicitType != null)
		{
			result = new VarStyleCategorySettings(
				VarStyleMode.UseExplicitType,
				hasConditions: false,
				conditionalForeach: false,
				conditionalLinqScalarResult: false,
				conditionalLinqCollectionResult: false,
				conditionalLinqAggregateResult: false,
				conditionalLongTypeName: false,
				DefaultLongTypeNameLength,
				conditionalEvident: false);
		}
		else
		{
			XElement? useVar = categoryElement.Element("UseVar");
			if (useVar != null)
			{
				bool hasForeach = useVar.Element("Foreach") != null;
				bool hasLinqScalarResult = useVar.Element("LinqScalarResult") != null;
				bool hasLinqCollectionResult = useVar.Element("LinqCollectionResult") != null;
				bool hasLinqAggregateResult = useVar.Element("LinqAggregateResult") != null;

				XElement? longTypeNameElement = useVar.Element("LongTypeName");
				bool hasLongTypeName = longTypeNameElement != null;
				int longTypeNameLength = DefaultLongTypeNameLength;
				if (hasLongTypeName)
				{
					string? lengthValue = longTypeNameElement!.Attribute("Length")?.Value;
					if (lengthValue != null && int.TryParse(lengthValue, out int parsedLength) && parsedLength > 0)
					{
						longTypeNameLength = parsedLength;
					}
				}

				bool hasEvident = useVar.Element("Evident") != null;
				bool hasConditions = hasForeach || hasLinqScalarResult || hasLinqCollectionResult
					|| hasLinqAggregateResult || hasLongTypeName || hasEvident;

				result = new VarStyleCategorySettings(
					VarStyleMode.UseVar,
					hasConditions,
					hasForeach,
					hasLinqScalarResult,
					hasLinqCollectionResult,
					hasLinqAggregateResult,
					hasLongTypeName,
					longTypeNameLength,
					hasEvident);
			}
			else
			{
				result = None;
			}
		}

		return result;
	}

	internal static VarStyleCategorySettings Resolve(AnalyzerConfigOptions options, string keyPrefix, VarStyleCategorySettings fallback)
	{
		VarStyleCategorySettings result = fallback;

		if (options.TryGetValue(keyPrefix, out string? modeValue) && !string.IsNullOrWhiteSpace(modeValue))
		{
			modeValue = modeValue.Trim();

			if (string.Equals(modeValue, "use_explicit_type", StringComparison.OrdinalIgnoreCase))
			{
				result = new VarStyleCategorySettings(
					VarStyleMode.UseExplicitType,
					hasConditions: false,
					conditionalForeach: false,
					conditionalLinqScalarResult: false,
					conditionalLinqCollectionResult: false,
					conditionalLinqAggregateResult: false,
					conditionalLongTypeName: false,
					DefaultLongTypeNameLength,
					conditionalEvident: false);
			}
			else if (string.Equals(modeValue, "use_var", StringComparison.OrdinalIgnoreCase))
			{
				bool hasForeach = GetBool(options, keyPrefix + ".foreach");
				bool hasLinqScalarResult = GetBool(options, keyPrefix + ".linq_scalar_result");
				bool hasLinqCollectionResult = GetBool(options, keyPrefix + ".linq_collection_result");
				bool hasLinqAggregateResult = GetBool(options, keyPrefix + ".linq_aggregate_result");
				bool hasLongTypeName = GetBool(options, keyPrefix + ".long_type_name");
				int longTypeNameLength = DefaultLongTypeNameLength;
				if (hasLongTypeName
					&& options.TryGetValue(keyPrefix + ".long_type_name_length", out string? lengthValue)
					&& int.TryParse(lengthValue, out int parsedLength) && parsedLength > 0)
				{
					longTypeNameLength = parsedLength;
				}

				bool hasEvident = GetBool(options, keyPrefix + ".evident");
				bool hasConditions = hasForeach || hasLinqScalarResult || hasLinqCollectionResult
					|| hasLinqAggregateResult || hasLongTypeName || hasEvident;

				result = new VarStyleCategorySettings(
					VarStyleMode.UseVar,
					hasConditions,
					hasForeach,
					hasLinqScalarResult,
					hasLinqCollectionResult,
					hasLinqAggregateResult,
					hasLongTypeName,
					longTypeNameLength,
					hasEvident);
			}
		}

		return result;
	}

	#endregion

	#region Private Methods

	private static bool GetBool(AnalyzerConfigOptions options, string key)
	{
		return options.TryGetValue(key, out string? value)
			&& (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1");
	}

	#endregion
}
