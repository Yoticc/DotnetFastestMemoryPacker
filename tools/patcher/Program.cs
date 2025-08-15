using dnlib.DotNet;
using System.Diagnostics;

try
{
    var currentDirectory = Environment.CurrentDirectory;

    string targetDirectory;
    if (Debugger.IsAttached)
        targetDirectory = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\..\..\src\DotnetFastestMemoryPacker\bin\Release\net9.0"));
    else targetDirectory = Path.GetFullPath(Path.Combine(currentDirectory, @".\bin\Release\net9.0"));

    var targetAssembly = Path.Combine(targetDirectory, "DotnetFastestMemoryPacker.dll");
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