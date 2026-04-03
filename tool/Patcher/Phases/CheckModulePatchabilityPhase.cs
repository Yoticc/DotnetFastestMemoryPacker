using dnlib.DotNet;

partial class Program
{
    bool IsModuleAlreadyPatched;
    void CheckModulePatchabilityPhase()
    {
        IsModuleAlreadyPatched = module.TypeExists("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", false);
    }
}