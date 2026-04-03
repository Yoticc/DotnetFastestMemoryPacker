using dnlib.DotNet;
using System.Diagnostics.CodeAnalysis;

partial class Program
{
    [AllowNull] ModuleDef corlibModule;
    [AllowNull] ModuleDef module;

    void ExecutePhase(string name, Action action)
    {
        Print($"** '{name}' start");
        IncreaseLoggerTab();
        action();
        DecreaseLoggerTab();
        Print($"** '{name}' end\n");
    }

    void ExecuteTask(string label, Action action)
    {
        Print($"> {label}");
        IncreaseLoggerTab();
        action();
        DecreaseLoggerTab();
    }

    void ExecuteAllPhases()
    {
        ExecutePhase("Check module patchability", CheckModulePatchabilityPhase);
        if (IsModuleAlreadyPatched)
            return;

        ExecutePhase("Define IgnoresAccessChecksToMethod", DefineIgnoresAccessChecksPhase);
        ExecutePhase("Inline transit methods", InlineTransitMethodsPhase);
        ExecutePhase("Implement unsafe accessors", ImplementUnsafeAccessorsPhase);
        ExecutePhase("Replace extrinsics with implementation", ReplaceExtrinsicsWithImplementationPhase);
    }
}