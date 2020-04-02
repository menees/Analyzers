namespace Menees.Analyzers.Test
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.Diagnostics;

	/// <summary>
	/// Location where the diagnostic appears, as determined by path, line number, and column number.
	/// </summary>
	[GeneratedCode("VS Template", "2017")]
	public struct DiagnosticResultLocation
	{
		public DiagnosticResultLocation(string path, int line, int column)
		{
			if (line < -1)
			{
				throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
			}

			if (column < -1)
			{
				throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
			}

			this.Path = path;
			this.Line = line;
			this.Column = column;
		}

		public string Path { get; }

		public int Line { get; }

		public int Column { get; }
	}

	/// <summary>
	/// Struct that stores information about a Diagnostic appearing in a source
	/// </summary>
	[GeneratedCode("VS Template", "2017")]
	public struct DiagnosticResult
	{
		private DiagnosticResultLocation[] locations;

		public DiagnosticResult(DiagnosticAnalyzer analyzer)
		{
			DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics.First();
			this.Severity = descriptor.DefaultSeverity;
			this.Id = descriptor.Id;
			this.Message = descriptor.MessageFormat.ToString();
			this.locations = null;
			this.Properties = null;
		}

		public DiagnosticResultLocation[] Locations
		{
			get
			{
				if (this.locations == null)
				{
					this.locations = new DiagnosticResultLocation[] { };
				}

				return this.locations;
			}

			set
			{
				this.locations = value;
			}
		}

		public DiagnosticSeverity Severity { get; set; }

		public string Id { get; set; }

		public string Message { get; set; }

		public string Path
		{
			get
			{
				return this.Locations.Length > 0 ? this.Locations[0].Path : string.Empty;
			}
		}

		public int Line
		{
			get
			{
				return this.Locations.Length > 0 ? this.Locations[0].Line : -1;
			}
		}

		public int Column
		{
			get
			{
				return this.Locations.Length > 0 ? this.Locations[0].Column : -1;
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Unit tests need to assign new dictionary.")]
		public Dictionary<string, string> Properties { get; set; }
	}
}
