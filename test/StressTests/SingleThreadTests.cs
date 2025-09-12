using DotnetFastestMemoryPacker;

namespace StressTests;
public unsafe class SingleThreadTests
{
    const int IterationCount = 1000;

    static readonly ClassWithGCPointer[] largeArrayOf_ClassWithGCPointer =
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

    /*
    [Fact]
    public void SerializeString()
    {
        char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        for (var iteration = 0; iteration < IterationCount; iteration++)
        {
            var input = new string(chars[Random.Shared.Next(0, chars.Length)], Random.Shared.Next(0, short.MaxValue));

            var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
            var deserialized = FastestMemoryPacker.Deserialize<string>(serialized);

            Assert.Equal(input, deserialized);
        }
    }
    */

    [Fact]
    public void SerializeLargeComplexObject()
    {
        var input = new ComplexObject1.A();
        for (var iteration = 0; iteration < IterationCount; iteration++)
        {
            GC.Collect();

            var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
            var deserialized = FastestMemoryPacker.Deserialize<ComplexObject1.A>(serialized);

            // too lazy to implement full comparison, so used partial
            Assert.Equal(input.StringField, deserialized.StringField);
            Assert.Equal(input.ListField[1].Value, deserialized.ListField[1].Value);
            Assert.Equal(input.ByteArrayArrayField[1][0], deserialized.ByteArrayArrayField[1][0]);
            Assert.Equal(input.MatrixIntArrayField[1, 1], deserialized.MatrixIntArrayField[1, 1]);
        }
    }
    
    /*
    [Fact]
    public void SerializeLargeArrayOfComplexObjects()
    {
        var input = largeArrayOf_ClassWithGCPointer;
        for (var iteration = 0; iteration < IterationCount / 100; iteration++)
        {
            var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
            var deserialized = FastestMemoryPacker.Deserialize<ClassWithGCPointer[]>(serialized);
        }
    }
    */

    class ComplexObject1
    {
        public class A_
        {
            public string StringField { get; set; } = "Some string here!";
            public string[] StringArrayField { get; set; } = ["index: 0", "index: 1"];

            public string EmptyStringField { get; set; } = "";
            public byte[] ByteArrayField { get; set; } = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF];
            public byte[][] ByteArrayArrayField { get; set; } = [[0x01, 0x02, 0x03, 0x04], [0x05], [0x06], [0x07]];
            public List<F> ListField { get; set; } = [new F() { Value = 101 }, new F() { Value = 102 }];
        }

        public class A : A_
        {
            static F FStatic = new F();

            public int[,] MatrixIntArrayField = new int[4, 4] { { 10, 11, 12, 13 }, { 14, 15, 16, 17 }, { 18, 19, 20, 21 }, { 22, 23, 24, 25 } };
            public F[,] MatrixFArrayField = new F[2, 2] { { FStatic, FStatic }, { FStatic, FStatic } };
            public int IntField1 = 0x7080;
            public ulong LongField = 0xE700660099661818UL;
            public int IntField2 = 0x0C;
            public B BField = new B();
            public D<double, double> DField1 = new D<double, double>() { TValue = 10d, T2Value = 20d };
            public D<string, F> DField2 = new D<string, F>() { TValue = "TValue!", T2Value = new F() { Value = 103 } };
        }

        public class B
        {
            public B() => SelfField = this;
            public B SelfField;
            public C<int> StructField = new() { XValue = 10, YValue = 20 };

            public (int, int) TupleField = (100, 100);
            public (int, F) TupleField2 = (100, new F());
        }

        public struct C<T> where T : unmanaged
        {
            public C() { }

            public T XValue;
            public T YValue;
            public long XYValue = 30;
        }

        public class F
        {
            public int Value = 100;
        }

        public class D<T, T2>
        {
            public T TValue;
            public T2 T2Value;
            public F FField = new F();
        }
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
}