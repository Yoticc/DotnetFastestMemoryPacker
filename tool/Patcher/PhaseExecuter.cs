using dnlib.DotNet;

class PhaseExecuter(ModuleDef module)
{
    void PreparePhase(Phase phase)
    {
        Phase.Dispatcher.SetWorker(phase, this);
        Phase.Dispatcher.SetModule(phase, module);
    }

    public void ExecuteLocalPhase(Phase phase)
    {
        PreparePhase(phase);

        Logger.PrintLocalPhase($"'{phase.PhaseName}' start");
        Logger.PushTab();
        phase.Execute();
        Logger.PopTab();
        Logger.PrintLocalPhase($"'{phase.PhaseName}' end\n");
    }

    public void ExecuteGlobalPhase(Phase phase)
    {
        PreparePhase(phase);

        Logger.PrintGlobalPhase($"'{phase.PhaseName}' start");
        Logger.PushTab();
        phase.Execute();
        Logger.PopTab();
        Logger.PrintGlobalPhase($"'{phase.PhaseName}' end\n");
    }

    public void ExecuteAllGlobalPhases()
    {
        var modulePatchabilityPhase = new CheckModulePatchabilityPhase();
        ExecuteGlobalPhase(modulePatchabilityPhase);
        if (modulePatchabilityPhase.IsAlreadyPatched)
            return;

        ExecuteGlobalPhase(new DefineIgnoresAccessChecksPhase());
    }
}