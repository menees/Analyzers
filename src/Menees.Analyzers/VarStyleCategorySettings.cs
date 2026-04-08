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

	#endregion
}
