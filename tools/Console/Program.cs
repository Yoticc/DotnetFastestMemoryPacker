unsafe class Program
{
    int a1, a2;

    static void Main() => new Program().InstanceMain();

    void InstanceMain()
    {
        new Thread(Stress).Start();

        PinnedMethod(this, []);
        Console.ReadLine();
    }

    static void Stress()
    {
        for (var i = 0; i < 1000; i++)
        {
            byte[] array = new byte[10];
            for (var o = 0; o < 1000; o++)
                array = new byte[500];

            Thread.Sleep(array.Length);
            GC.Collect();
        }
    }

    static void PinnedMethod(object @object, byte[] dummyArray)
    {
        fixed (byte* dummyArrayPointer = dummyArray)
        {
            *&dummyArrayPointer = *(byte**)&@object;

            Thread.Sleep(400);

            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(250);
                DisplayObject(@object);
            }
        }
    }

    static void DisplayObject(object @object)
    {
        Console.WriteLine($"{*(nint*)&@object:X}");
    }

    static void TestMethod(byte[] array)
    {
        fixed (byte* pointer = array)
        {
            Console.WriteLine((nint)pointer);
        }
    }
}