using dnlib.DotNet;

abstract record AbstractTask
{
    public abstract void Execute();
    public abstract string GetMessage();
}

abstract record ModuleTask(ModuleDef module) : AbstractTask, ITaskable
{
    public ModuleDef Module => module;
}

abstract record TypeTask(TypeDef type) : ModuleTask(type.Module);

interface ITask : ITaskable
{
    void Execute();
    string GetMessage();
}

readonly ref struct DefineMethodTask(TypeDef type, MethodDef method) : ITask
{
    public ModuleDef Module => type.Module;

    public void Execute()
    {
        type.Methods.Add(method);
    }

    public string GetMessage() => $"Define method '{method.Name}' in type '{type.Name}'";
}