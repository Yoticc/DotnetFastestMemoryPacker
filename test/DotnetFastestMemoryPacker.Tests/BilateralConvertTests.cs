using System.Buffers;
using System.Runtime.CompilerServices;

namespace DotnetFastestMemoryPacker.Tests;
public unsafe class BilateralConvertTests
{
    static BilateralConvertTests()
    {
        explicitRecursiveReference = new ExplicitRecursiveReference
        {
            Value1 = 1200,
            Value2 = 2100
        };
        explicitRecursiveReference.Self = explicitRecursiveReference;

        implicitRecursiveReference0 = new ImplicitRecursiveReference0
        {
            Value1 = 0909
        };

        implicitRecursiveReference1 = new ImplicitRecursiveReference1
        {
            String = "Waiting for recaf 5. Day 600",
            Value1 = 9061,
            Parent = implicitRecursiveReference0
        };

        implicitRecursiveReference0.SomeElement = implicitRecursiveReference1;
    }

    static readonly UnmanagedStruct4 unmanagedStruct4 = new UnmanagedStruct4
    {
        Value1 = 901327461849171L,
        Value2 = 524381661529101L,
        Value3 = 208332764279421L,
        Value4 = 298339263729171L,
        Value5 = 001339268939481L,
        Value6 = 641397561619291L
    };

    static readonly UnmanagedStruct3 unmanagedStruct3 = new UnmanagedStruct3
    {
        Struct4 = unmanagedStruct4,
        Struct42 = unmanagedStruct4,
        Value1 = false,
        Value2 = false
    };

    static readonly UnmanagedStruct2 unmanagedStruct2 = new UnmanagedStruct2
    {
        Struct3 = unmanagedStruct3,
        Struct4 = unmanagedStruct4,
        Value1 = 2947928748,
        Value2 = 24987
    };

    static readonly UnmanagedStruct1 unmanagedStruct1 = new UnmanagedStruct1
    {
        Struct2 = unmanagedStruct2,
        Struct3 = unmanagedStruct3,
        Value1 = 80,
        Value2 = 2908744792,
        Value3 = 299403920
    };

    static readonly UnmanagedStruct0 unmanagedStruct0 = new UnmanagedStruct0
    {
        Struct1 = unmanagedStruct1,
        Value1 = 0xB00B1E3,
        Value2 = 0xDEAD40E
    };

    static readonly ManagedStruct1 managedStruct1 = new ManagedStruct1
    {
        StringValue = "Ооо-оо-оо ооо-оо-оо time to fuckin' die",
        Value1 = 0x404080,
        Value2 = 0x909010
    };

    static readonly ManagedStruct0 managedStruct0 = new ManagedStruct0
    {
        ManagedStruct1 = managedStruct1,
        StringValue = "They all remember my name. They are afraid that they will become like this"
    };

    static readonly int[] intArray200 = Enumerable.Range(0, 200).ToArray();

    static readonly string[] stringArray200 = Enumerable.Range(0, 200).Select(i => $"{i}").ToArray();

    static readonly ClassWithGCPointer[] arrayOf_ClassWithGCPointer_200 =
        Enumerable.Range(0, 200)
        .Select(i =>
        new ClassWithGCPointer
        {
            Value1 = i,
            SimpleClass = new SimpleClass
            {
                Value1 = i * 2,
                Value2 = (i * 2) << 8,
            }
        })
        .ToArray();

    static readonly ClassWithGCPointer[] arrayOf_ClassWithGCPointer_80000 =
        Enumerable.Range(0, 80000)
        .Select(i =>
        new ClassWithGCPointer
        {
            Value1 = i,
            SimpleClass = new SimpleClass
            {
                Value1 = i * 2,
                Value2 = (i * 2) << 8,
            }
        })
        .ToArray();

    static readonly ExplicitRecursiveReference explicitRecursiveReference;

    static readonly ImplicitRecursiveReference1 implicitRecursiveReference1;

    static readonly ImplicitRecursiveReference0 implicitRecursiveReference0;

    [Fact]
    public void Serialize_UnmanagedStruct()
    {
        var input = unmanagedStruct0;

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<UnmanagedStruct0>(serialized);

        Assert.Equal(input, deserialized);
    }

    [Fact]
    public void Serialize_ManagedStruct()
    {
        var input = managedStruct0;

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<ManagedStruct0>(serialized);

        Assert.Equal(input, deserialized);
    }

    [Fact]
    public void Serialize_Null()
    {
        object? input = null;

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<object>(serialized);

        Assert.Empty(serialized);
        Assert.Null(deserialized);
    }

    [Fact]
    public void Serialize_EmptyObject()
    {
        var input = new object();

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<object>(serialized);

        Assert.Equal(4, serialized.Length);
        Assert.NotNull(deserialized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("1234567890")]
    public void Serialize_String(string input)
    {
        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<string>(serialized);

        Assert.Equal(input, deserialized);
    }

    [Fact]
    public void Serialize_NullableStruct_Null()
    {
        var input = new StructNullableContainer
        {
            UnmanagedStruct4 = null
        };

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<StructNullableContainer>(serialized);

        Assert.Equal(input, deserialized);
    }

    [Fact]
    public void Serialize_NullableStruct_NotNull()
    {
        var input = new StructNullableContainer
        {
            UnmanagedStruct4 = unmanagedStruct4
        };

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<StructNullableContainer>(serialized);

        Assert.Equal(input, deserialized);
    }

    [Fact]
    public void Serialize_Array_NoGCPointers()
    {
        var input = intArray200;

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<int[]>(serialized);

        Assert.Equal(input, deserialized);
    }

    [Fact]
    public void Serialize_Array_String()
    {
        var input = stringArray200;

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<string[]>(serialized);

        Assert.Equal(input, deserialized);
    }

    [Fact]
    public void Serialize_Array200_WithGCPointers()
    {
        var input = arrayOf_ClassWithGCPointer_200;

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<ClassWithGCPointer[]>(serialized);

        for (var i = 0; i < input.Length; i++)
        {
            var a = input[i];
            var b = deserialized[i];

            Assert.Equal(a.Value1, b.Value1);
            if (a is null && b is null)
                continue;

            Assert.NotNull(a);
            Assert.NotNull(b);

            var a2 = a.SimpleClass;
            var b2 = b.SimpleClass;
            Assert.NotNull(a2);
            Assert.NotNull(b2);

            Assert.Equal(a2.Value1, b2.Value1);
            Assert.Equal(a2.Value2, b2.Value2);
        }
    }

    [Fact]
    public void Serialize_Array80000_WithGCPointers()
    {
        var input = arrayOf_ClassWithGCPointer_80000;

        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        var deserialized = FastestMemoryPacker.Deserialize<ClassWithGCPointer[]>(serialized);

        for (var i = 0; i < input.Length; i++)
        {
            var a = input[i];
            var b = deserialized[i];

            Assert.Equal(a.Value1, b.Value1);
            if (a is null && b is null)
                continue;

            Assert.NotNull(a);
            Assert.NotNull(b);

            var a2 = a.SimpleClass;
            var b2 = b.SimpleClass;
            Assert.NotNull(a2);
            Assert.NotNull(b2);

            Assert.Equal(a2.Value1, b2.Value1);
            Assert.Equal(a2.Value2, b2.Value2);
        }
    }

    /*
    [Fact]
    public void Serialize_RecursiveReference_Explicit()
    {
        var input = explicitRecursiveReference;

        var serialized = FastestMemoryPacker.Serialize(input);
        var deserialized = FastestMemoryPacker.Deserialize<ExplicitRecursiveReference>(serialized);

        Assert.True(deserialized == deserialized.Self);
        Assert.Equal(input.Value2, deserialized.Value2);
        Assert.Equal(input.Value1, deserialized.Value1);
    }

    [Fact]
    public void Serialize_RecursiveReference_Implicit()
    {
        var input = implicitRecursiveReference0;

        var serialized = FastestMemoryPacker.Serialize(input);
        var deserialized = FastestMemoryPacker.Deserialize<ImplicitRecursiveReference0>(serialized);

        Assert.NotNull(deserialized.SomeElement);

        Assert.True(deserialized == deserialized.SomeElement.Parent);
        Assert.Equal(input.Value1, deserialized.Value1);

        var a = input.SomeElement!;
        var b = deserialized.SomeElement;
        Assert.Equal(a.Value1, b.Value1);
        Assert.Equal(a.String, b.String);
    }
    */

    struct ManagedStruct0
    {
        public string StringValue;
        public ManagedStruct1 ManagedStruct1;
    }

    struct ManagedStruct1
    {
        public long Value1, Value2;
        public string StringValue;
    }

    struct UnmanagedStruct0
    {
        public int Value1;
        public UnmanagedStruct1 Struct1;
        public int Value2;
    }

    struct UnmanagedStruct1
    {
        public int Value1;
        public UnmanagedStruct2 Struct2;
        public UnmanagedStruct3 Struct3;
        public long Value2;
        public long Value3;
    }

    struct UnmanagedStruct2
    {
        public long Value1;
        public long Value2;
        public UnmanagedStruct3 Struct3;
        public UnmanagedStruct4 Struct4;
    }

    struct UnmanagedStruct3
    {
        public bool Value1;
        public bool Value2;
        public UnmanagedStruct4 Struct4;
        public UnmanagedStruct4 Struct42;
    }

    struct UnmanagedStruct4
    {
        public long Value1;
        public long Value2;
        public long Value3;
        public long Value4;
        public long Value5;
        public long Value6;
    }

    struct StructNullableContainer
    {
        public UnmanagedStruct4? UnmanagedStruct4;
    }

    class ClassWithGCPointer
    {
        public long Value1;
        public SimpleClass? SimpleClass;
    }

    class SimpleClass
    {
        public int Value1;
        public int Value2;
    }

    class ExplicitRecursiveReference
    {
        public int Value1;
        public int Value2;
        public ExplicitRecursiveReference? Self;
    }

    class ImplicitRecursiveReference0
    {
        public int Value1;
        public ImplicitRecursiveReference1? SomeElement;
    }

    class ImplicitRecursiveReference1
    {
        public int Value1;
        public string? String;
        public ImplicitRecursiveReference0? Parent;
    }
}