namespace Menees.Analyzers.Test;

// This code started from StyleCopAnalyzers-master\StyleCop.Analyzers\StyleCop.Analyzers.Test\Settings\SettingsUnitTests.cs
internal sealed class AdditionalTextHelper(string path, string text) : AdditionalText
{
	private readonly SourceText sourceText = SourceText.From(text);

	public override string Path { get; } = path;

	public override SourceText GetText(CancellationToken cancellationToken = default) => this.sourceText;
}
