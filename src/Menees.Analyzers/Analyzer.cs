namespace Menees.Analyzers;

public abstract class Analyzer : DiagnosticAnalyzer
{
	#region Private Data Members

	// Use a volatile field to ensure thread-safe reads/writes since EnableConcurrentExecution
	// allows multiple threads to run analysis callbacks concurrently on the same analyzer instance.
	private volatile Settings settings = Settings.Default;

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

	internal Settings Settings
	{
		get => this.settings;
		set => this.settings = value;
	}

	#endregion
}
