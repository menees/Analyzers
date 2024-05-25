namespace Menees.Analyzers;

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
		// I tried to use context.RegisterCompilationStartAction to initialize this.Settings.
		// Unfortunately, that start action didn't always run before other actions that need
		// settings (e.g., SyntaxNodeActions).
	}

	#endregion

	#region Internal Properties

	internal Settings Settings { get; set; }

	#endregion
}
