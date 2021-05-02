namespace Menees.Analyzers.Test
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.Text;

	#endregion

	// This code came from StyleCopAnalyzers-master\StyleCop.Analyzers\StyleCop.Analyzers.Test\Settings\SettingsUnitTests.cs
	internal sealed class AdditionalTextHelper : AdditionalText
	{
		private readonly SourceText sourceText;

		public AdditionalTextHelper(string path, string text)
		{
			this.Path = path;
			this.sourceText = SourceText.From(text);
		}

		public override string Path { get; }

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			return this.sourceText;
		}
	}
}
