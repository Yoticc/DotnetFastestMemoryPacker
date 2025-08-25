using dnlib.DotNet;

try
{
    var currentDirectory = Environment.CurrentDirectory;
    var targetAssemblies = Directory.GetFiles(currentDirectory, "*.dll", SearchOption.AllDirectories);
    foreach (var targetAssembly in targetAssemblies)
    {
        if (Path.GetFileName(targetAssembly) != "DotnetFastestMemoryPacker.dll")
            continue;

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