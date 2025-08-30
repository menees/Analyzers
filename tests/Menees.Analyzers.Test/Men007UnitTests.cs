namespace Menees.Analyzers.Test;

[TestClass]
public class Men007UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men007UseSingleReturn();

	#endregion

	#region ValidCodeTest

	[TestMethod]
	public void ValidCodeTest()
	{
		this.VerifyCSharpDiagnostic(string.Empty);

		const string test = @"using System;
class Testing
{
	/// <summary>Test</summary>
	public Testing()
	{
	}

	public DateTime Now
	{
		get
		{
			return DateTime.Now;
		}
	}

	public int this[int index]
	{
		get
		{
			return index;
		}
	}

	public Testing Create()
	{
		if (Convert.ToBoolean(0))
		{
			Func<bool> test = () => { return false; };
			test();
		}

		return new Testing();
	}

	public void DoItTwice()
	{
		string DoOp(object arg)
		{
			return arg?.ToString();
		}

		DoOp(1);
		DoOp(2);
	}

	public Testing CreateLocal()
	{
		string DoOp(object arg)
		{
			return arg?.GetHashCode().ToString();
		}

		Testing result = new Testing();
		DoOp(result);
		return result;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region InvalidCodeTest

	[TestMethod]
	public void InvalidCodeTest()
	{
		const string test = @"using System;
class Testing
{
	public DateTime Now
	{
		get
		{
			if (Convert.ToBoolean(0))
			{
				return DateTime.MinValue;
			}

			return DateTime.Now;
		}
	}

	public int this[int index]
	{
		get
		{
			if (Convert.ToBoolean(0))
			{
				return -1;
			}

			return index;
		}
	}

	public Testing Create()
	{
		if (Convert.ToBoolean(0))
		{
			// The lambda's return statement shouldn't be counted.
			Func<bool> test = () => { return false; };
			test();

			return null;
		}

		return new Testing();
	}

	private void Check()
	{
		if (Convert.ToBoolean(0))
		{
			return;
		}

		Create();
	}

	public void TestLocalFunctions()
	{
		void DoOp(object arg)
		{
			arg?.ToString();
			return;
		}

		string DoOp2(object arg)
		{
			if (arg == null)
				return ""NULL"";
			else
				return arg.ToString();
		}

		DoOp(1);
		DoOp2(2);

		Func<bool> test = () => { if (Convert.ToBoolean(0)) return false; else return true; };
		test();

		Action<bool> test2 = x => { x.ToString(); return; };
		test2(false);
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Multiple return statements (2) are used in get_Now.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 6, 3),
					new DiagnosticResultLocation("Test0.cs", 10, 5),
					new DiagnosticResultLocation("Test0.cs", 13, 4),
				],
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Multiple return statements (2) are used in get_Item.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 19, 3),
					new DiagnosticResultLocation("Test0.cs", 23, 5),
					new DiagnosticResultLocation("Test0.cs", 26, 4),
				]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Multiple return statements (2) are used in Create.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 30, 2),
					new DiagnosticResultLocation("Test0.cs", 38, 4),
					new DiagnosticResultLocation("Test0.cs", 41, 3),
				]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "A return statement is used in Check, which returns void.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 44, 2),
					new DiagnosticResultLocation("Test0.cs", 48, 4),
				]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "A return statement is used in DoOp, which returns void.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 56, 3),
					new DiagnosticResultLocation("Test0.cs", 59, 4),
				]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Multiple return statements (2) are used in DoOp2.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 62, 3),
					new DiagnosticResultLocation("Test0.cs", 65, 5),
					new DiagnosticResultLocation("Test0.cs", 67, 5),
				]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "Multiple return statements (2) are used in <ParenthesizedLambdaExpression>.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 73, 21),
					new DiagnosticResultLocation("Test0.cs", 73, 55),
					new DiagnosticResultLocation("Test0.cs", 73, 74),
				]
			},
			new DiagnosticResult(analyzer)
			{
				Message = "A return statement is used in <SimpleLambdaExpression>, which returns void.",
				Locations =
				[
					new DiagnosticResultLocation("Test0.cs", 76, 24),
					new DiagnosticResultLocation("Test0.cs", 76, 45),
				]
			},
		];

		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion
}