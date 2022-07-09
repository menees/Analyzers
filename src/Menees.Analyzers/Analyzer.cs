namespace Menees.Analyzers
{
	public abstract class Analyzer : DiagnosticAnalyzer
	{
		#region Public Methods

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterCompilationStartAction(startContext => { this.Settings = Settings.Cache(startContext); });
		}

		#endregion

		#region Internal Properties

		internal Settings Settings { get; private set; }

		#endregion
	}
}
