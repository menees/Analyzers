namespace Menees.Analyzers
{
	public abstract class Analyzer : DiagnosticAnalyzer
	{
		#region Constructors

		protected Analyzer()
		{
			// Ensure Settings is never null.
			this.Settings = Settings.Default;
		}

		#endregion

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
