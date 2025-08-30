using dnlib.DotNet;

try
{
    var currentDirectory = Environment.CurrentDirectory;

#if DEBUG
    while (!Path.Exists(Path.Combine(currentDirectory!, "src")))
        currentDirectory = Path.GetDirectoryName(currentDirectory);
#endif

    var targetAssemblies = Directory.GetFiles(currentDirectory, "DotnetFastestMemoryPacker.dll", SearchOption.AllDirectories);
    foreach (var targetAssembly in targetAssemblies)
    {
        if (targetAssembly.Contains(@"\Debug\"))
            continue;

        Console.WriteLine($"Target assembly: \"{targetAssembly}\"");

        using var fileStream = new FileStream(targetAssembly, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        var assembly = ModuleDefMD.Load(fileStream);

        new Patcher().Execute(assembly);

        fileStream.SetLength(0);
        fileStream.Position = 0;
        assembly.Write(fileStream);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.ReadLine();
}
