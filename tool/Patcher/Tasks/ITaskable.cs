using dnlib.DotNet;
using dnlib.DotNet.Emit;

interface ITaskable
{
    ModuleDef Module { get; }
}

static class ITaskableExtensions
{
    /* utils */
    public static ITypeDefOrRef GetCorlibTypeRef(this ITaskable self, string @namespace, string name)
    {
        return new Importer(self.Module).Import(self.GetCorlibTypeDef(@namespace, name));
    }

    public static TypeDef GetCorlibTypeDef(this ITaskable self, string @namespace, string name) 
    {
        return self.Module.CorLibTypes.GetTypeRef(@namespace, name).Resolve();
    }

    /* tasks */
    public static TypeDefUser DefineType(this ITaskable self, string @namespace, string name, ITypeDefOrRef baseType)
    {
        var fullname = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

        NotifyTaskStart($"Define type '{fullname}'");
        var module = self.Module;
        var definedType = new TypeDefUser(@namespace, name, baseType);
        module.AddAsNonNestedType(definedType);
        NotifyTaskEnd();

        return definedType;
    }

    public static MethodDefUser DefineMethod(
        this ITaskable self, 
        TypeDef declaringType, 
        TypeSig returnTypeSignature, 
        TypeSig[] argumentSignatures, 
        MethodAttributes attributes, 
        string name)
    {
        var methodSignature = MethodSig.CreateInstance(returnTypeSignature, argumentSignatures);
        return self.DefineMethod(declaringType, methodSignature, attributes, name);
    }

    public static MethodDefUser DefineMethod(this ITaskable self, TypeDef declaringType, MethodSig signature, MethodAttributes attributes, string name)
    {
        NotifyTaskStart($"Define empty constructor for type '{declaringType.Name}'");
        var module = self.Module;
        var ctorSignature = MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String);
        var definedMethod = new MethodDefUser(name, signature, MethodImplAttributes.IL, attributes);
        definedMethod.Body = new CilBody();

        declaringType.Methods.Add(definedMethod);
        NotifyTaskEnd();

        return definedMethod;
    }

    static void NotifyTaskStart(string taskMessage)
    {
        Logger.PrintTask(taskMessage);
        Logger.PushTab();
    }

    static void NotifyTaskEnd()
    {
        Logger.PopTab();
    }
}