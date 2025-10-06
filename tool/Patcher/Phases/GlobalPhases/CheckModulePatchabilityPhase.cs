using dnlib.DotNet;

record CheckModulePatchabilityPhase() : Phase("Check module patchability")
{
    public bool IsAlreadyPatched { get; private set; }

    public override void Execute()
    {
        if (Module.TypeExists("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", false))
            IsAlreadyPatched = true;
    }
}