namespace Menees.Analyzers.Test
{
	[TestClass]
	public sealed class Men002UnitTests : CodeFixVerifier
	{
		#region Protected Properties

		protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men002LineTooLong();

		#endregion

		#region ValidCodeTest

		[TestMethod]
		public void ValidCodeTest()
		{
			this.VerifyCSharpDiagnostic(string.Empty);

			const string test = @"
using System;

namespace Test
{
	class Testing
	{
		/// <summary>Test</summary>
		/// <see href=""https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d314-see"" />
		/// <seealso  href=""https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d314-see""  />
		/// <see href='https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d314-see' />
		/// <seealso  href='https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d314-see'  />
		public Testing()
		{
			// LongUriLines=true
			//// This is a long line preceded by four slashes. Setting AllowLongFourSlashCommentLines=true makes it valid.
			// https://www.amazon.com/Brain-Games%C2%AE-Large-Print-Searches/dp/1640304606/ref=sr_1_2?crid=3KP7CV3HBJADN&keywords=big+long+search+text&qid=1645375366&sprefix=big+long+search+text%2Caps%2C72&sr=8-2
			/// https://www.amazon.com/Brain-Games%C2%AE-Large-Print-Searches/dp/1640304606/ref=sr_1_2?crid=3KP7CV3HBJADN&keywords=big+long+search+text&qid=1645375366&sprefix=big+long+search+text%2Caps%2C72&sr=8-2
			//// https://www.amazon.com/Brain-Games%C2%AE-Large-Print-Searches/dp/1640304606/ref=sr_1_2?crid=3KP7CV3HBJADN&keywords=big+long+search+text&qid=1645375366&sprefix=big+long+search+text%2Caps%2C72&sr=8-2
			/* https://www.google.com/maps/place/Yellowstone+National+Park/@44.5854032,-111.0744669,9z/data=!3m1!4b1!4m5!3m4!1s0x5351e55555555555:0xaca8f930348fe1bb!8m2!3d44.427963!4d-110.588455 */
			/** https://www.google.com/maps/place/Yellowstone+National+Park/@44.5854032,-111.0744669,9z/data=!3m1!4b1!4m5!3m4!1s0x5351e55555555555:0xaca8f930348fe1bb!8m2!3d44.427963!4d-110.588455 */
			/*** https://www.google.com/maps/place/Yellowstone+National+Park/@44.5854032,-111.0744669,9z/data=!3m1!4b1!4m5!3m4!1s0x5351e55555555555:0xaca8f930348fe1bb!8m2!3d44.427963!4d-110.588455 **/
			/*
				Brazil weather?
				https://weather.com/weather/today/l/63e18eea74a484c42c3921cf52a8fec98113dbb13f6deb7c477b2f453c95b837
				Or:
				\\weatherserver\brazil\sao\paulo\today\63e18eea74a484c42c3921cf52a8fec98113dbb13f6deb7c477b2f453c95b837
			*/
		}
	}
}";
			this.VerifyCSharpDiagnostic(test);
		}

		#endregion

		#region InvalidCodeTest

		[TestMethod]
		public void InvalidCodeTest()
		{
			const string test = @"
namespace ConsoleApplication1
{
	using System;
	using System.Collections.Generic; // This line runs on much much much too long.

	class TypeName
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <seealso      cref='System.Text.RegularExpressions.Regex'  />
		public TypeName() // This line is also much too long.
		{
			// AllowLongUriLines=true
			// GT fans: https://www.amazon.com/yourfanshop?selectedTeamName=Georgia%20Tech%20Yellow%20Jackets&asin=&refinement=popular&team=375636011
		}
	}
}";
			var analyzer = this.CSharpDiagnosticAnalyzer;
			DiagnosticResult[] expected =
			[
				new DiagnosticResult(analyzer)
				{
					Message = "Line must be no longer than 40 characters (now 83).",
					Locations = [new DiagnosticResultLocation("Test0.cs", 5, 38)]
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Line must be no longer than 40 characters (now 73).",
					Locations = [new DiagnosticResultLocation("Test0.cs", 12, 35)]
				},
				new DiagnosticResult(analyzer)
				{
					Message = "Line must be no longer than 40 characters (now 61).",
					Locations = [new DiagnosticResultLocation("Test0.cs", 13, 35)]
				},
#if DEBUG // MEN002A is disabled by default, so it won't run in release build unit tests.
				new DiagnosticResult(analyzer)
				{
					Id = Men002LineTooLong.DiagnosticIdNotify,
					Severity = DiagnosticSeverity.Info,
					Message = "Line is over 35 characters (now 37).",
					Locations = [new DiagnosticResultLocation("Test0.cs", 15, 27)],
				},
#endif
				new DiagnosticResult(analyzer)
				{
					Message = "Line must be no longer than 40 characters (now 149).",
					Locations = [new DiagnosticResultLocation("Test0.cs", 16, 32)]
				},
			];

			this.VerifyCSharpDiagnostic(test, expected);
		}

		#endregion
	}
}