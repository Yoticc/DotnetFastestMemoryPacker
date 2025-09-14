using DotnetFastestMemoryPacker;
using System;

unsafe class Program
{
    static bool hasMainThread = true;
    static void Main()
    {
        new Thread(MonitorGC).Start();

        FastestMemoryPacker.TestMethod();
        Thread.Sleep(-1);

        var input = new A();
        var serialized = FastestMemoryPacker.SerializeWithObjectIdentify(input);
        File.WriteAllBytes(@"C:\a.txt", serialized);
        for (var i = 0; i < 1000000; i++)
        {
            Console.WriteLine(i);
            GC.Collect();
            var deserialized = FastestMemoryPacker.Deserialize<A>(serialized);

            _ = 3;
        }

        Console.WriteLine("Completed");
        Console.ReadLine();
        hasMainThread = false;
    }

    class SimpleClass
    {
        public int Value1;
        public int Value2;
    }

    class ClassWithGCPointer
    {
        public long Value1;
        public SimpleClass? SimpleClass;
    }

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

    static int gen0;
    static int gen1;
    static int gen2;
    static void MonitorGC()
    {
        gen0 = GC.CollectionCount(0);
        gen1 = GC.CollectionCount(1);
        gen2 = GC.CollectionCount(2);
        
        while (hasMainThread)
        {
            Thread.Sleep(1);
            Cycle();
        }

        static void Cycle()
        {
            var ngen0 = GC.CollectionCount(0);
            var dgen0 = ngen0 - gen0;
            if (dgen0 > 0)
            {
                //Console.WriteLine($"GC collected Gen0 {dgen0} times");
                gen0 = ngen0;
            }

            var ngen1 = GC.CollectionCount(1);
            var dgen1 = ngen1 - gen1;
            if (dgen1 > 0)
            {
                //Console.WriteLine($"GC collected Gen1 {dgen1} times");
                gen1 = ngen1;
            }

            var ngen2 = GC.CollectionCount(2);
            var dgen2 = ngen2 - gen2;
            if (dgen2 > 0)
            {
                //Console.WriteLine($"GC collected Gen2 {dgen2} times");
                gen2 = ngen2;
            }
        }
    }
}

class A_
{
    public string StringField { get; set; } = "Some string here!";
    public string[] StringArrayField { get; set; } = ["index: 0", "index: 1"];
    
    public string EmptyStringField { get; set; } = "";
    public byte[] ByteArrayField { get; set; } = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF];
    public byte[][] ByteArrayArrayField { get; set; } = [[0x01, 0x02, 0x03, 0x04], [0x05], [0x06], [0x07]];
    public List<F> ListField { get; set; } = [new F() { Value = 101 }, new F() { Value = 102 }];
}

class A : A_
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

class B
{
    public B() => SelfField = this;
    public B SelfField;
    //public C<int> StructField = new() { XValue = 10, YValue = 20 };
    //public (int, int) TupleField = (100, 100);
    //public (int, F) TupleField2 = (100, new F());
}

struct C<T> where T : unmanaged
{
    public C() { }

    public T XValue;
    public T YValue;
    public long XYValue = 30;
}

class F
{
    public int Value = 100;
}

partial class D<T, T2>
{
    public T TValue;
    public T2 T2Value;
    public F FField = new F();
}