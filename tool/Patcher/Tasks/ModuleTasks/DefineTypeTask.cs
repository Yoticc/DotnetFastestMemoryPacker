using dnlib.DotNet;

record DefineTypeTask(ModuleDef module, string @namespace, string name, ITypeDefOrRef baseType) : ModuleTask(module)
{
    public override void Execute()
    {
        var type = new TypeDefUser(@namespace, name, baseType);
        module.AddAsNonNestedType(type);
    }

    public override string GetMessage()
    {
        var fullname = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
        return $"Define type '{fullname}'";
    }
}