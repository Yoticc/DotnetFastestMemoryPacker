using dnlib.DotNet;

try
{
    var currentDirectory = Environment.CurrentDirectory;

    while (true)
    {
        if (Path.GetFileName(currentDirectory) == "DotnetFastestMemoryPacker")
            if (Directory.Exists(Path.Combine(currentDirectory, "src")))
                break;

        currentDirectory = Path.GetDirectoryName(currentDirectory)!;
    }

    var targetAssembly = Path.Combine(currentDirectory, @"src\DotnetFastestMemoryPacker\bin\Release\net9.0\DotnetFastestMemoryPacker.dll");
    using var fileStream = new FileStream(targetAssembly, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    
    var assembly = ModuleDefMD.Load(fileStream);

    new Patcher().Execute(assembly);

    fileStream.SetLength(0);
    fileStream.Position = 0;
    assembly.Write(fileStream);
} 
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.ReadLine();
}