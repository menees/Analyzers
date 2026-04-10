namespace Menees.Analyzers.Test.Helpers;

internal sealed class TestAnalyzerConfigOptionsProvider(AnalyzerConfigOptions treeOptions) : AnalyzerConfigOptionsProvider
{
	public override AnalyzerConfigOptions GlobalOptions => treeOptions;

	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => treeOptions;

	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => treeOptions;
}
