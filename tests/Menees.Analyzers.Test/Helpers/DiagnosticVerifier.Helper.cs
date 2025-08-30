namespace Menees.Analyzers.Test;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Class for turning strings into documents and getting the diagnostics on them
/// All methods are static
/// </summary>
[GeneratedCode("VS Template", "2017")]
public abstract partial class DiagnosticVerifier
{
	private static readonly MetadataReference[] RequiredTypeReferences = [.. new Type[]
	{
		typeof(Compilation),
		typeof(CSharpCompilation),
		typeof(Enumerable),
		typeof(object),
	}.Select(CreateTypeReference)];

	private static readonly MetadataReference[] RequiredLibraryReferences = [.. new string[]
	{
		"netstandard.dll",
		"System.Runtime.dll",
	}.Select(CreateLibraryReference)];

	internal static readonly ThreadLocal<string> DefaultFilePathPrefix = new(() => "Test");
	internal const string CSharpDefaultFileExt = "cs";
	internal const string TestProjectName = "TestProject";

	#region  Get Diagnostics

	/// <summary>
	/// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
	/// </summary>
	/// <param name="sources">Classes in the form of strings</param>
	/// <param name="language">The language the source classes are in</param>
	/// <param name="analyzer">The analyzer to be run on the sources</param>
	/// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
	private Diagnostic[] GetSortedDiagnostics(string[] sources, string language, DiagnosticAnalyzer analyzer)
	{
		return GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(sources, language));
	}

	/// <summary>
	/// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
	/// The returned diagnostics are then ordered by location in the source document.
	/// </summary>
	/// <param name="analyzer">The analyzer to run on the documents</param>
	/// <param name="documents">The Documents that the analyzer will be run on</param>
	/// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
	protected static Diagnostic[] GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents)
	{
		HashSet<Project> projects = [];
		foreach (Document document in documents)
		{
			projects.Add(document.Project);
		}

		AdditionalText additionalText = new AdditionalTextHelper("Menees.Analyzers.Settings.xml", Properties.Resources.Menees_Analyzers_Settings_Xml);
#pragma warning disable IDE0303 // Simplify collection initialization. NET Framework 4.8 doesn't support collection expressions for ImmutableArray.Create.
		AnalyzerOptions options = new(ImmutableArray.Create(additionalText));
		List<Diagnostic> diagnostics = [];
		foreach (Project project in projects)
		{
			Compilation? compilation = project.GetCompilationAsync().Result;
			ReportCompilationDiagnostics(compilation);
			CompilationWithAnalyzers? compilationWithAnalyzers = compilation?.WithAnalyzers(ImmutableArray.Create(analyzer), options);
#pragma warning restore IDE0303 // Simplify collection initialization

			IEnumerable<Diagnostic> diags = compilationWithAnalyzers?.GetAnalyzerDiagnosticsAsync().Result ?? Enumerable.Empty<Diagnostic>();
			foreach (Diagnostic diag in diags)
			{
				if (diag.Location == Location.None || diag.Location.IsInMetadata)
				{
					diagnostics.Add(diag);
				}
				else
				{
					for (int i = 0; i < documents.Length; i++)
					{
						Document document = documents[i];
						SyntaxTree? tree = document.GetSyntaxTreeAsync().Result;
						if (tree == diag.Location.SourceTree)
						{
							diagnostics.Add(diag);
						}
					}
				}
			}
		}

		Diagnostic[] results = SortDiagnostics(diagnostics);
		diagnostics.Clear();
		return results;
	}

	#endregion

	#region Set up compilation and documents

	private static void ReportCompilationDiagnostics(Compilation? compilation)
	{
		ImmutableArray<Diagnostic>? compilationDiagnostics = compilation?.GetDiagnostics();
		if (compilationDiagnostics != null)
		{
			Diagnostic[] reportable = [.. compilationDiagnostics.Value
				.Where(d => d.Severity > DiagnosticSeverity.Hidden)
				.OrderBy(d => d.Location.GetLineSpan().Span.Start.Line)];
			if (reportable.Length > 0)
			{
				const DiagnosticSeverity FailSeverity = DiagnosticSeverity.Warning;
				foreach (Diagnostic diagnostic in reportable.Where(d => d.Severity < FailSeverity))
				{
					Debug.WriteLine(diagnostic);
				}

				Diagnostic[] errors = [.. reportable.Where(d => d.Severity >= FailSeverity)];
				if (errors.Length > 0)
				{
					string nl = Environment.NewLine;
					throw new ArgumentException($"Compile errors:{nl}{string.Join(nl, (IEnumerable<Diagnostic>)errors)}");
				}
			}
		}
	}

	/// <summary>
	/// Sort diagnostics by location in source document
	/// </summary>
	/// <param name="diagnostics">The list of Diagnostics to be sorted</param>
	/// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
	private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
		=> [.. diagnostics.OrderBy(d => d.Location.SourceSpan.Start)];

	/// <summary>
	/// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
	/// </summary>
	/// <param name="sources">Classes in the form of strings</param>
	/// <param name="language">The language the source code is in</param>
	/// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
	private Document[] GetDocuments(string[] sources, string language)
	{
		if (language != LanguageNames.CSharp)
		{
			throw new ArgumentException("Unsupported Language");
		}

		Project project = CreateProject(sources, language);
		Document[] documents = [.. project.Documents];

		if (sources.Length != documents.Length)
		{
			throw new InvalidOperationException("Amount of sources did not match amount of Documents created");
		}

		return documents;
	}

	/// <summary>
	/// Create a Document from a string through creating a project that contains it.
	/// </summary>
	/// <param name="source">Classes in the form of a string</param>
	/// <param name="language">The language the source code is in</param>
	/// <returns>A Document created from the source string</returns>
	protected Document CreateDocument(string source, string language = LanguageNames.CSharp)
	{
		return CreateProject([source], language).Documents.First();
	}

	/// <summary>
	/// Create a project using the inputted strings as sources.
	/// </summary>
	/// <param name="sources">Classes in the form of strings</param>
	/// <param name="language">The language the source code is in</param>
	/// <returns>A Project created out of the Documents created from the source strings</returns>
	private Project CreateProject(string[] sources, string language = LanguageNames.CSharp)
	{
		string fileNamePrefix = DefaultFilePathPrefix.Value ?? string.Empty;
		string fileExt = CSharpDefaultFileExt;

		ProjectId projectId = ProjectId.CreateNewId(debugName: TestProjectName);

		Solution solution = new AdhocWorkspace()
			.CurrentSolution
			.AddProject(projectId, TestProjectName, TestProjectName, language);

		IEnumerable<MetadataReference> metadataReferences = RequiredTypeReferences
			.Concat(RequiredLibraryReferences)
			.Concat(AssemblyRequiredTypes.Select(CreateTypeReference))
			.Concat(AssemblyRequiredLibraryFileNames.Select(CreateLibraryReference));
		foreach (MetadataReference? reference in metadataReferences)
		{
			solution = solution.AddMetadataReference(projectId, reference);
		}

		int count = 0;
		foreach (string source in sources)
		{
			string newFileName = fileNamePrefix + count + "." + fileExt;
			DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
			solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
			count++;
		}

		Project? project = solution.GetProject(projectId);
		if (project?.ParseOptions is CSharpParseOptions parseOptions)
		{
			bool isNetFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework ");
			parseOptions = parseOptions.WithPreprocessorSymbols(isNetFramework ? "NETFRAMEWORK" : "NET")
				.WithLanguageVersion(LanguageVersion.Latest);
			solution = solution.WithProjectParseOptions(projectId, parseOptions);
		}

		if (project?.CompilationOptions is CompilationOptions options)
		{
			// Don't use a ConsoleApplication since we don't have a Main() method in each test.
			// https://davidwalschots.github.io/2019/11/12/fixing-roslyn-compilation-errors-in-unit-tests-of-a-code-analyzer
			solution = solution.WithProjectCompilationOptions(projectId, options.WithOutputKind(this.AssemblyOutputKind));
		}

		return solution.GetProject(projectId) ?? throw new InvalidOperationException("Unable to create project.");
	}

	private static PortableExecutableReference CreateTypeReference(Type type)
		=> MetadataReference.CreateFromFile(type.Assembly.Location);

	private static PortableExecutableReference CreateLibraryReference(string fileName)
		=> MetadataReference.CreateFromFile(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), fileName));

	#endregion
}