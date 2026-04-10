namespace Menees.Analyzers.Test.Helpers;

internal sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options) : AnalyzerConfigOptions
{
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
	public override bool TryGetValue(string key, out string? value) => options.TryGetValue(key, out value!);
#pragma warning restore CS8765

	public override IEnumerable<string> Keys => options.Keys;
}
