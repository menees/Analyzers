namespace Menees.Analyzers.Test;

// Test settings: BuiltInTypes=UseExplicitType, SimpleTypes=UseVar, Elsewhere=UseVar with conditions
// (Foreach, LinqScalarResult, LinqCollectionResult, LinqAggregateResult, LongTypeName 30, Evident).
// Conditions are child elements of UseVar; their presence implies they're enabled.
[TestClass]
public class Men020UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override CodeFixProvider CSharpCodeFixProvider => new Men020UsePreferredVarStyleFixer();

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men020UsePreferredVarStyle();

	protected override IEnumerable<Type> AssemblyRequiredTypes => [typeof(List<>)];

	#endregion

	#region Valid Code Tests

	[TestMethod]
	public void EmptyCode()
	{
		this.VerifyCSharpDiagnostic(string.Empty);
	}

	[TestMethod]
	public void ExplicitBuiltInType()
	{
		const string test = @"
class C
{
	string M()
	{
		int x = 5;
		string s = ""hello"";
		bool b = true;
		return s + b + x;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentSimpleTypeNewExpression()
	{
		const string test = @"
class MyClass { }
class C
{
	void M()
	{
		var x = new MyClass();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentSimpleTypeCast()
	{
		const string test = @"
class MyClass { }
class C
{
	void M(object obj)
	{
		var x = (MyClass)obj;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentSimpleTypeAsExpression()
	{
		const string test = @"
class MyClass { }
class C
{
	void M(object obj)
	{
		var x = obj as MyClass;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentEnumMember()
	{
		const string test = @"
enum Color { Red, Green, Blue }
class C
{
	Color M()
	{
		var c = Color.Red;
		return c;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentStaticProperty()
	{
		// Static property returning the declaring type is evident (e.g., Encoding.UTF8, CultureInfo.InvariantCulture).
		const string test = @"
class Singleton
{
	public static Singleton Instance { get; } = new Singleton();
}
class C
{
	Singleton M()
	{
		var s = Singleton.Instance;
		return s;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentDefaultExpression()
	{
		const string test = @"
class MyClass { }
class C
{
	MyClass M()
	{
		var x = default(MyClass);
		return x;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentArrayCreation()
	{
		const string test = @"
class MyClass { }
class C
{
	void M()
	{
		var arr = new MyClass[5];
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForGenericType()
	{
		const string test = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		var list = new List<int>();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void AnonymousType()
	{
		const string test = @"
class C
{
	void M()
	{
		var x = new { A = 1, B = ""hello"" };
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void ForeachWithVarSimpleType()
	{
		const string test = @"
using System.Collections.Generic;
class MyItem { }
class C
{
	void M()
	{
		var items = new List<MyItem>();
		foreach (var item in items)
		{
		}
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void ConstDeclaration()
	{
		const string test = @"
class C
{
	int M()
	{
		const int x = 5;
		return x;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void MultipleDeclarators()
	{
		const string test = @"
class C
{
	int M()
	{
		int a = 1, b = 2;
		return a + b;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void ExplicitSimpleType()
	{
		// UseVar mode flags explicit type when var could be used.
		const string test = @"
class MyClass { }
class C
{
	MyClass GetMyClass() => new MyClass();
	void M()
	{
		MyClass m = GetMyClass();
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'var' instead of 'MyClass'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForLinqScalarResult()
	{
		const string test = @"
using System.Collections.Generic;
using System.Linq;
class C
{
	void M()
	{
		var items = new List<KeyValuePair<string, int>>();
		var first = items.First();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForLinqCollectionResult()
	{
		const string test = @"
using System.Collections.Generic;
using System.Linq;
class MyClass { public int Value { get; set; } }
class C
{
	void M()
	{
		var items = new List<MyClass>();
		var filtered = items.Where(x => x.Value > 0);
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForLinqAggregateResult()
	{
		// Aggregate with a generic seed type returns a generic type (Elsewhere → UseVar with conditions).
		const string test = @"
using System.Collections.Generic;
using System.Linq;
class C
{
	List<int> M()
	{
		var items = new[] { 1, 2, 3 };
		var result = items.Aggregate(new List<int>(), (acc, x) => { acc.Add(x); return acc; });
		return result;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForLongTypeName()
	{
		// Dictionary<string, List<string>> is 33 chars > 30 threshold.
		const string test = @"
using System.Collections.Generic;
class C
{
	Dictionary<string, List<string>> Get() => new Dictionary<string, List<string>>();
	void M()
	{
		var x = Get();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentGenericArrayType()
	{
		// Array of generic type → Elsewhere → conditional UseVar.
		// Each element is an ObjectCreationExpression → Evident condition met.
		const string test = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		var x = new[] { new KeyValuePair<string, int>(""a"", 1) };
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForEvidentConversionMethod()
	{
		// ToString returns string (built-in type → UseExplicitType), but we test
		// with a custom ToMyClass conversion method which returns a simple type.
		const string test = @"
class MyClass
{
	public static MyClass ToMyClass(object obj) => new MyClass();
}
class C
{
	MyClass M(object obj)
	{
		var x = MyClass.ToMyClass(obj);
		return x;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void IgnoreGeneratedCode()
	{
		const string test = @"
// <auto-generated>
class C
{
	int M()
	{
		var x = 5;
		return x;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void ExplicitBuiltInForLoop()
	{
		const string test = @"
class C
{
	int M()
	{
		int sum = 0;
		for (int i = 0; i < 10; i++)
		{
			sum += i;
		}
		return sum;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void OutVarSimpleType()
	{
		// Simple type → UseVar (unconditional) → var is OK.
		const string test = @"
class MyClass { }
class C
{
	bool TryGet(out MyClass value) { value = new MyClass(); return true; }
	void M()
	{
		if (TryGet(out var value))
		{
		}
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void DeconstructionWithVarSimpleTypes()
	{
		// Each var resolves to a simple type → UseVar (unconditional) → var is OK.
		const string test = @"
class MyClass { public void Deconstruct(out MyClass a, out MyClass b) { a = this; b = this; } }
class C
{
	void M()
	{
		var obj = new MyClass();
		(var x, var y) = obj;
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	#endregion

	#region Invalid Code Tests

	[TestMethod]
	public void VarForBuiltInInt()
	{
		const string test = @"
class C
{
	int M()
	{
		var x = 5;
		return x;
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'int' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForBuiltInString()
	{
		const string test = @"
class C
{
	string M()
	{
		var s = ""hello"";
		return s;
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'string' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForBuiltInBool()
	{
		const string test = @"
class C
{
	bool M()
	{
		var b = true;
		return b;
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'bool' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForBuiltInNonNullableString()
	{
		// In a nullable context, var infers string (NotNull flow state) → should suggest 'string', not 'string?'.
		const string test = @"
#nullable enable
class C
{
	string M()
	{
		var x = ""test"";
		return x;
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'string' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForBuiltInNullableString()
	{
		// In a nullable context, var infers string? (MaybeNull flow state) → should suggest 'string?'.
		const string test = @"
#nullable enable
class C
{
	string? GetNullable() => null;
	string? M()
	{
		var x = GetNullable();
		return x;
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'string?' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void ExplicitGenericType()
	{
		// Explicit type in conditional UseVar mode is always OK.
		const string test = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		List<int> list = new List<int>();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForNonEvidentSimpleType()
	{
		// UseVar mode allows var for all simple types.
		const string test = @"
class MyClass { }
class C
{
	MyClass GetMyClass() => new MyClass();
	void M()
	{
		var m = GetMyClass();
	}
}";
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void ExplicitGenericForeach()
	{
		// Elsewhere mode (conditional UseVar) allows explicit generic types.
		const string test = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		var list = new List<int>();
		foreach (int item in list)
		{
		}
	}
}";
		// int is built-in → UseExplicitType mode → explicit is OK, no diagnostic expected.
		this.VerifyCSharpDiagnostic(test);
	}

	[TestMethod]
	public void VarForBuiltInForeach()
	{
		// Built-in type in foreach with UseExplicitType mode → diagnostic even in foreach.
		const string test = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		var list = new List<int>();
		foreach (var item in list)
		{
		}
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'int' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 12)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void MultipleDiagnosticsInOneMethod()
	{
		const string test = @"
using System.Collections.Generic;
class MyClass { }
class C
{
	MyClass GetMyClass() => new MyClass();
	string M()
	{
		var x = 5;
		var m = GetMyClass();
		Dictionary<string, int> dict = new Dictionary<string, int>();
		return x + """" + m + dict.Count;
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'int' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 9, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForNonEvidentGenericType()
	{
		// Conditional UseVar mode flags var when no condition is met.
		const string test = @"
using System.Collections.Generic;
class C
{
	List<int> GetList() => new List<int>();
	void M()
	{
		var list = GetList();
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'List<int>' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForNonEvidentGenericArrayType()
	{
		// Array of generic type → Elsewhere → conditional UseVar.
		// kvp is an identifier (not evident), and KeyValuePair<string, int>[] is 27 chars < 30 threshold.
		const string test = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		var kvp = new KeyValuePair<string, int>(""a"", 1);
		var x = new[] { kvp };
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'KeyValuePair<string, int>[]' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 3)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void VarForBuiltInForLoop()
	{
		// int is BuiltIn → UseExplicitType → var should be flagged.
		const string test = @"
class C
{
	int M()
	{
		int sum = 0;
		for (var i = 0; i < 10; i++)
		{
			sum += i;
		}
		return sum;
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'int' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 7, 8)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void OutVarBuiltInType()
	{
		// int is BuiltIn → UseExplicitType → out var should be flagged.
		const string test = @"
class C
{
	void M()
	{
		int.TryParse(""5"", out var result);
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'int' instead of 'var'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 6, 25)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	[TestMethod]
	public void ExplicitSimpleTypeOutParam()
	{
		// Simple type → UseVar (unconditional) → explicit type should be flagged.
		const string test = @"
class MyClass { }
class C
{
	bool TryGet(out MyClass value) { value = new MyClass(); return true; }
	void M()
	{
		if (TryGet(out MyClass value))
		{
		}
	}
}";
		DiagnosticAnalyzer analyzer = this.CSharpDiagnosticAnalyzer;
		DiagnosticResult[] expected =
		[
			new DiagnosticResult(analyzer)
			{
				Message = "Use 'var' instead of 'MyClass'.",
				Locations = [new DiagnosticResultLocation("Test0.cs", 8, 18)],
			},
		];
		this.VerifyCSharpDiagnostic(test, expected);
	}

	#endregion

	#region Code Fix Tests

	[TestMethod]
	public void FixVarToExplicitBuiltInType()
	{
		const string test = @"
class C
{
	int M()
	{
		var x = 5;
		return x;
	}
}";
		const string fixtest = @"
class C
{
	int M()
	{
		int x = 5;
		return x;
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixExplicitToVarSimpleType()
	{
		const string test = @"
class MyClass { }
class C
{
	MyClass GetMyClass() => new MyClass();
	void M()
	{
		MyClass m = GetMyClass();
	}
}";
		const string fixtest = @"
class MyClass { }
class C
{
	MyClass GetMyClass() => new MyClass();
	void M()
	{
		var m = GetMyClass();
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixVarToExplicitGenericType()
	{
		const string test = @"
using System.Collections.Generic;
class C
{
	List<int> GetList() => new List<int>();
	void M()
	{
		var list = GetList();
	}
}";
		const string fixtest = @"
using System.Collections.Generic;
class C
{
	List<int> GetList() => new List<int>();
	void M()
	{
		List<int> list = GetList();
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixVarToExplicitBuiltInForeach()
	{
		const string test = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		var list = new List<int>();
		foreach (var item in list)
		{
		}
	}
}";
		const string fixtest = @"
using System.Collections.Generic;
class C
{
	void M()
	{
		var list = new List<int>();
		foreach (int item in list)
		{
		}
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixMultipleDiagnostics()
	{
		const string test = @"
class C
{
	int M()
	{
		var x = 5;
		var s = ""hello"";
		return x + s.Length;
	}
}";
		const string fixtest = @"
class C
{
	int M()
	{
		int x = 5;
		string s = ""hello"";
		return x + s.Length;
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixVarToExplicitBuiltInForLoop()
	{
		const string test = @"
class C
{
	int M()
	{
		int sum = 0;
		for (var i = 0; i < 10; i++)
		{
			sum += i;
		}
		return sum;
	}
}";
		const string fixtest = @"
class C
{
	int M()
	{
		int sum = 0;
		for (int i = 0; i < 10; i++)
		{
			sum += i;
		}
		return sum;
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixOutVarToExplicitBuiltInType()
	{
		const string test = @"
class C
{
	void M()
	{
		int.TryParse(""5"", out var result);
	}
}";
		const string fixtest = @"
class C
{
	void M()
	{
		int.TryParse(""5"", out int result);
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixExplicitToVarOutParam()
	{
		const string test = @"
class MyClass { }
class C
{
	bool TryGet(out MyClass value) { value = new MyClass(); return true; }
	void M()
	{
		if (TryGet(out MyClass value))
		{
		}
	}
}";
		const string fixtest = @"
class MyClass { }
class C
{
	bool TryGet(out MyClass value) { value = new MyClass(); return true; }
	void M()
	{
		if (TryGet(out var value))
		{
		}
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixVarToExplicitNullableBuiltInType()
	{
		const string test = @"
#nullable enable
class C
{
	string? GetNullable() => null;
	string? M()
	{
		var x = GetNullable();
		return x;
	}
}";
		const string fixtest = @"
#nullable enable
class C
{
	string? GetNullable() => null;
	string? M()
	{
		string? x = GetNullable();
		return x;
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	[TestMethod]
	public void FixVarToExplicitNonNullableBuiltInType()
	{
		const string test = @"
#nullable enable
class C
{
	string M()
	{
		var x = ""test"";
		return x;
	}
}";
		const string fixtest = @"
#nullable enable
class C
{
	string M()
	{
		string x = ""test"";
		return x;
	}
}";
		this.VerifyCSharpFix(test, fixtest);
	}

	#endregion
}
