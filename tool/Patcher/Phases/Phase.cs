using dnlib.DotNet;
using System.Diagnostics.CodeAnalysis;

abstract record Phase(string phaseName) : ITaskable
{
    [AllowNull] private protected PhaseExecuter Worker { get; private set; }
    [AllowNull] public ModuleDef Module { get; private set; }

    public string PhaseName => phaseName;

    public abstract void Execute();

    private protected void Execute<T>(T subPhase) where T : Phase => Worker.ExecuteLocalPhase(subPhase);

    // fuck factories, they are for workers. i mma unemploeth so as all ma homi i use dispatchers 
    public static class Dispatcher
    {
        public static void SetWorker(Phase phase, PhaseExecuter worker) => phase.Worker = worker;

        public static void SetModule(Phase phase, ModuleDef module) => phase.Module = module;
    }
}