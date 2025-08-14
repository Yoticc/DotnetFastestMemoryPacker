using dnlib.DotNet;

try
{
    var currentDirectory = Environment.CurrentDirectory;

    var targetDirectory = Path.GetFullPath(Path.Combine(currentDirectory, @".\bin\Debug\net9.0")); 
    if (!Directory.Exists(targetDirectory))
        targetDirectory = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\..\..\src\DotnetFastestMemoryPacker\bin\Debug\net9.0")); // via debugger

    var targetAssembly = Path.Combine(targetDirectory, "DotnetFastestMemoryPacker.dll");
    using var fileStream = new FileStream(targetAssembly, FileMode.OpenOrCreate, FileAccess.ReadWrite);
    
    var assembly = ModuleDefMD.Load(fileStream);

    new Patcher().Execute(assembly);

    assembly.Write(fileStream);
} 
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.ReadLine();
}