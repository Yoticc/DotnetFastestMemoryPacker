using dnlib.DotNet;
using System.Diagnostics.CodeAnalysis;

abstract record Phase(string phaseName) : ITaskable
{
    [AllowNull] private protected PhaseExecutor Executor { get; private set; }
    [AllowNull] public ModuleDef Module { get; private set; }

    public string PhaseName => phaseName;

    public abstract void Execute();

    private protected void Execute<T>(T subPhase) where T : Phase => Executor.ExecuteLocalPhase(subPhase);

    // fuck factories, they are for workers. i mma unemploeth so as all ma homi i use dispatchers 
    public static class Dispatcher
    {
        public static void SetExecutor(Phase phase, PhaseExecutor executor) => phase.Executor = executor;

        public static void SetModule(Phase phase, ModuleDef module) => phase.Module = module;
    }
}