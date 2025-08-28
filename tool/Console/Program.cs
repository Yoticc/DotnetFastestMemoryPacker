using DotnetFastestMemoryPacker;

unsafe class Program
{
    static void Main()
    {
        var input = new A();

        var serialized = FastestMemoryPacker.Serialize(input);
        File.WriteAllBytes(@"C:\a.txt", serialized);
        //var deserialized = FastestMemoryPacker.Deserialize<A>(serialized);

        Console.ReadLine();
        _ = 3;
    }
}

class A_
{
    public string StringField { get; set; } = "Some string here!";
    public string[] StringArrayField { get; set; } = ["index: 0", "index: 1"];
    public string EmptyStringField { get; set; } = "";
    public byte[] ByteArrayField { get; set; } = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF];
    public byte[][] ByteArrayArrayField { get; set; } = [[0x01, 0x02, 0x03, 0x04], [0x05], [0x06], [0x07]];
    public List<F> ListField { get; set; } = [new F(), new F()];
}

class A : A_
{
    static F FStatic = new F();

    public int[,] MatrixIntArrayField = new int[4, 4] { { 4, 4, 4, 4 }, { 4, 4, 4, 4 }, { 4, 4, 4, 4 }, { 4, 4, 4, 4 } };
    public F[,] MatrixFArrayField = new F[2, 2] { { FStatic, FStatic }, { FStatic, FStatic } };
    public int IntField1 = 0x7080;
    public ulong LongField = 0xE700660099661818UL;
    public int IntField2 = 0x0C;
    public B BField = new B();
}

class B
{
    public B() => SelfField = this;

    public B SelfField;
    public C StructField = new();

    public (int, int) TupleField = (100, 100);
    public (int, F) TupleField2 = (100, new F());
}

struct C
{
    public C() { }

    public int XValue = 10;
    public int YValue = 20;
    public long XYValue = 30;
}

class F
{
    public int Value = 100;
}

partial class D
{
    public int IntValue;
    public int IntValue2;
    public long LongValue;
    public long LongValue2;
    public short ShortValue;
    public short ShortValue2;
    public short ShortValue3;
    public short ShortValue4;
    public bool BoolValue;
    public bool BoolValue2;
}