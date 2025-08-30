namespace Menees.Analyzers.Test;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menees.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using static System.Net.Mime.MediaTypeNames;

// This was inspired by Dustin Campbell's UseGetterOnlyAutoPropertyAnalyzer, which was abandoned in 2015.
// https://github.com/DustinCampbell/CSharpEssentials/blob/master/Source/CSharpEssentials.Tests/GetterOnlyAutoProperty/UseGetterOnlyAutoPropertyAnalyzerTests.cs
// https://github.com/DustinCampbell/CSharpEssentials/blob/master/Source/CSharpEssentials.Tests/GetterOnlyAutoProperty/UseGetterOnlyAutoPropertyCodeFixTests.cs
[TestClass]
public class Men017UnitTests : CodeFixVerifier
{
	#region Protected Properties

	protected override CodeFixProvider CSharpCodeFixProvider => new Men017RemoveUnusedPrivateSetterFixer();

	protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new Men017RemoveUnusedPrivateSetter();

	protected override IEnumerable<Type> AssemblyRequiredTypes => [typeof(BrowsableAttribute)];

	#endregion

	#region Valid Code Tests

	[TestMethod]
	public void UsedInMethodInPartialType()
	{
		const string Code = """
			partial class C
			{
			    public int Property { get; private set; }
			}

			partial class C
			{
			    public void M()
			    {
			        Property = 0;
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void ExplicitInterfaceProperty()
	{
		const string Code = """
			interface I
			{
			    int Property { get; set; }
			}

			class C : I
			{
			    int I.Property { get; set; }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void ReadOnly()
	{
		const string Code = """
			class C
			{
			    public bool Property { get; }
			    public C(bool f)
			    {
			        Property = f;
			    }
			}
			""";
		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void SetterHasBody()
	{
		const string Code = """
			class C
			{
			    public bool Property {
			        get { return false; }
			        private set { ; }
			    }

			    public C(bool f)
			    {
			        Property = f;
			    }
			}
			""";
		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void UsedInMethod()
	{
		const string Code = """
			class C
			{
			    public bool Property { get; private set; }
			    public void Method()
			    {
			        Property = false;
			    }
			}
			""";
		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void PostIncrementAssignment()
	{
		const string Code = """
			class C
			{
			    public int Property { get; private set; }
			    public void Method()
			    {
			        Property++;
			    }
			}
			""";
		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void PreDecrementAssignment()
	{
		const string Code = """
			class C
			{
			    public int Property { get; private set; }
			    public void Method()
			    {
			        --Property;
			    }
			}
			""";
		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void OrAssignment()
	{
		const string Code = """
			class C
			{
			    public int Property { get; private set; }
			    public void Method()
			    {
			        Property |= 0x1;
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void CoalesceAssignment()
	{
		const string Code = """
			class C
			{
			    public int? Property { get; private set; }
			    public void Method()
			    {
			        Property ??= 0x1;
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void RightShiftAssignment()
	{
		const string Code = """
			class C
			{
			    public int Property { get; private set; }
			    public void Method()
			    {
			        Property >>= 1;
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void NestedExpressionAssignment()
	{
		const string Code = """
			class C
			{
			    public int Property { get; private set; }
			    public float Method()
			    {
			        float y = 0;
			        for (int i = 0; i < 10; i++)
			        {
			            y += (((float)(unchecked((Property) = i))));
			        }
			        return y;
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void NestedConstructorAssignment()
	{
		const string Code = """
			class C
			{
			    public bool Property { get; private set; }
			    private class Nested
			    {
			        public Nested(C c)
			        {
			            c.Property = false;
			        }
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void UsedInLambda()
	{
		const string Code = """
			using System;
			class C
			{
			    public int Property { get; private set; }

			    public C()
			    {
			        Action f = new Action(() => Property = 2);
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void UsedInLocalFunction()
	{
		const string Code = """
			class C
			{
			    public int Property { get; private set; }

			    public C()
			    {
			        void f() => Property = 2;
					f();
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void StaticAssignedByInstanceConstructor()
	{
		const string Code = """
			class C
			{
			    public static int Property { get; private set; }

			    public C()
			    {
			        Property = 2;
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void IgnoreGeneratedCode()
	{
		const string Code = """
			// <auto-generated>
			class C
			{
			    public bool Property { get; private set; }
			    public C(bool f)
			    {
			        Property = f;
			    }
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void HasAttribute()
	{
		const string Code = """
			class C
			{
				[System.ComponentModel.Browsable(true)]
			    public bool Property { get; private set; }
			    public C(bool f)
			    {
			        Property = f;
			    }
			}
			""";
		this.VerifyCSharpDiagnostic(Code);
	}

	[TestMethod]
	public void ConstructorWithCustomSetter()
	{
		// https://github.com/menees/Analyzers/issues/12
		const string Code = """
			#nullable enable
			using System.Collections.Generic;
			using System.Runtime.CompilerServices;
			public sealed class MyClass
			{
			    private readonly Dictionary<string, object?> myMap = new();

			    public MyClass(string myString) => this.MyString = myString;

			    public string MyString
			    {
			        get => this.Read<string>()!;
			        private set => this.Write(value);
			    }

			    private TValue Read<TValue>([CallerMemberName] string propertyName = "") =>
			        this.myMap.TryGetValue(propertyName, out var value) ? (TValue)value! : default!;

			    private void Write<TValue>(TValue value, [CallerMemberName] string propertyName = "") =>
			        this.myMap[propertyName] = value;
			}
			""";

		this.VerifyCSharpDiagnostic(Code);
	}

	#endregion

	#region Invalid Code Tests

	[TestMethod]
	public void UsedInConstructorOnly()
	{
		const string Code = """
			class C
			{
			    public bool Property { get; [|private set;|] }
			    public C(bool f)
			    {
			        Property = f;
			    }
			}
			""";

		RequireDiagnostic(Code);
	}

	[TestMethod]
	public void UsedInConstructorOnlyPartial()
	{
		const string Code = """
			partial class C
			{
			    public int Property { get; [|private set;|] }
			}

			partial class C
			{
			    public C()
			    {
			        Property = 0;
			    }
			}
			""";

		RequireDiagnostic(Code);
	}

	[TestMethod]
	public void ReferencedButNotAssigned()
	{
		const string Code = """
			class C
			{
			    public int Property { get; [|private set;|] }
			    public void Method()
			    {
			        int x = 0;
			        x += Property;
			    }
			}
			""";

		RequireDiagnostic(Code);
	}

	[TestMethod]
	public void UsedInMultipleConstructors()
	{
		const string Code = """
			class C
			{
			    public bool Property { get; [|private set;|] }
			    public C()
			    {
			        Property = false;
			    }

			    public C(bool b)
			    {
			        Property = b;
			    }
			}
			""";

		RequireDiagnostic(Code);
	}

	[TestMethod]
	public void UsedInConstructorReadElsewhere()
	{
		const string Code = """
			class C
			{
			    public bool Property { get; [|private set;|] }
			    public C()
			    {
			        Property = false;
			    }

			    bool NotProp()
			    {
			        return !Property;
			    }
			}
			""";

		RequireDiagnostic(Code);
	}

	[TestMethod]
	public void StaticAssignedByStaticConstructor()
	{
		const string Code = """
			class C
			{
			    public static int Property { get; [|private set;|] }

			    static C()
			    {
			        Property = 2;
			    }
			}
			""";

		RequireDiagnostic(Code);
	}

	#endregion

	#region Code Fix Tests

	[TestMethod]
	public void FixSimpleProperty()
	{
		const string Original = """
			class C
			{
			    public bool P1 { get; private set; }
			}
			""";

		const string Fixed = """
			class C
			{
			    public bool P1 { get; }
			}
			"""
		;

		this.VerifyCSharpFix(Original, Fixed);
	}

	[TestMethod]
	public void FixSetterBeforeGetter()
	{
		const string Original = """
			class C
			{
			    public bool P1 { private set; get; }
			}
			""";

		const string Fixed = """
			class C
			{
			    public bool P1 { get; }
			}
			"""
		;

		this.VerifyCSharpFix(Original, Fixed);
	}

	[TestMethod]
	public void FixMultiline()
	{
		const string Original = """
			class C
			{
			    public bool P1
			    {
			        get;
			        private set;
			    }
			}
			""";

		// We have to jump through some hoops to remove the blank line without
		// messing up the whitespace inside or after the accessor list.
		const string Fixed = """
			class C
			{
			    public bool P1
			    {
			        get;
			    }
			}
			"""
		;

		this.VerifyCSharpFix(Original, Fixed);
	}

	[TestMethod]
	public void FixPreservesComments()
	{
		const string Original = """
			class C
			{
			    public bool P1
			    {
			        get; // Getter comment
			        private set; // Setter comment
			    }
			}
			""";

		const string Fixed = """
			class C
			{
			    public bool P1
			    {
			        get; // Getter comment
			             // Setter comment
			    }
			}
			"""
		;

		this.VerifyCSharpFix(Original, Fixed);
	}

	#endregion

	#region Private Methods

	private void RequireDiagnostic(string markupCode, string propertyName = "Property")
	{
		const string Prefix = "[|";
		const string Suffix = "|]";

		DiagnosticResult[] expected = [];
		using StringReader reader = new(markupCode);
		string? line;
		int lineNumber = 1;
		while ((line = reader.ReadLine()) != null)
		{
			int prefixIndex = line.IndexOf(Prefix);
			if (prefixIndex >= 0)
			{
				int suffixIndex = line.IndexOf(Suffix, prefixIndex + Prefix.Length);
				if (suffixIndex < 0)
				{
					throw new ArgumentException($"Suffix {Suffix} was not found on line {lineNumber} with prefix {Prefix}.");
				}

				expected =
				[
					new DiagnosticResult(this.CSharpDiagnosticAnalyzer)
					{
						Message = $"Remove the unused private set accessor from the {propertyName} auto property.",
						Locations = [new DiagnosticResultLocation("Test0.cs", lineNumber, prefixIndex + 1)]
					},
				];

				break;
			}

			lineNumber++;
		}

		string code = markupCode.Replace(Prefix, string.Empty).Replace(Suffix, string.Empty);
		this.VerifyCSharpDiagnostic(code, expected);
	}

	#endregion
}
