using dnlib.DotNet;

interface ITaskable
{
    ModuleDef Module { get; }
}

static class ITaskableExtensions
{
    public static ITypeDefOrRef GetCorlibTypeRef(this ITaskable self, string @namespace, string name) => new Importer(self.Module).Import(self.GetCorlibTypeDef(@namespace, name));

    public static TypeDef GetCorlibTypeDef(this ITaskable self, string @namespace, string name) => self.Module.CorLibTypes.GetTypeRef(@namespace, name).Resolve();

    public static void DefineType(this ITaskable self, string @namespace, string name, ITypeDefOrRef baseType) => self.Execute<DefineTypeTask>(@namespace, name, baseType);

    public static void DefineMethod(this ITaskable self, TypeDef declaringType, MethodDef method)
    {

    }

    public static void Execute<T>(this ITaskable self, TypeDef type, params object[] arguments) where T : TypeTask
        => ExecuteTask<T>(self, [self.Module, .. arguments]);

    public static void Execute<T>(this ITaskable self, params object[] arguments) where T : ModuleTask
        => ExecuteTask<T>(self, [self.Module, .. arguments]);

    static void ExecuteTask<T>(ITaskable taskable, object[] arguments) where T : AbstractTask
    {
        var taskInstance = CreateTaskInstance(arguments);
        var taskMessage = taskInstance.GetMessage();

        Logger.PrintTask(taskMessage);
        Logger.PushTab();
        taskInstance.Execute();
        Logger.PopTab();

        static AbstractTask CreateTaskInstance(object[] arguments)
        {
            var taskType = typeof(T);
            var constructors = taskType.GetConstructors();
            if (constructors.Length == 0)
                throw new Exception($"Task '{taskType.Name}' does not have any constructor.");

            var taskInstance = Activator.CreateInstance(taskType, arguments) as AbstractTask;
            if (taskInstance is null)
                throw new Exception($"Failed to create an instance for task '{taskType.Name}'");

            return taskInstance;
        }
    }
}