using dnlib.DotNet;
using PatcherReference;

static class PatcherProvider
{
    static CustomAttribute? GetAttibute(IHasCustomAttribute memberOwner, string name)
    {
        var attributes = memberOwner.CustomAttributes;
        var attribute = attributes.FirstOrDefault(attribute => attribute.AttributeType.Name == name);
        return attribute;
    }

    public static CustomAttribute? GetUnsafeAccessAttribute(MethodDef methodOwner) => GetAttibute(methodOwner, nameof(UnsafeAccessAttribute));

    public static bool HasUnsafeAccessAttribute(MethodDef methodOwner) => GetUnsafeAccessAttribute(methodOwner) is not null;

    public static bool GetUnsafeAccessAttributeArguments(MethodDef methodOwner, out string typeName, out string methodName, out string? methodSignature)
    {
        (typeName, methodName, methodSignature) = (null!, null!, null);

        var unsafeAccessAttribute = GetUnsafeAccessAttribute(methodOwner);
        if (unsafeAccessAttribute is null)
            return false;

        var args = unsafeAccessAttribute.ConstructorArguments;
        typeName = args[0].Value as UTF8String;
        if (typeName is null)
            throw new Exception("HandleUnsafeAccessors: 1-th arg, type name, is null");

        methodName = args[1].Value as UTF8String;
        if (methodName is null)
            methodName = methodOwner.Name;

        methodSignature = (UTF8String)args[2].Value;

        return true;
    }

    public static CustomAttribute? GetTransitMethodAttribute(MethodDef methodOwner) => GetAttibute(methodOwner, nameof(InlineAttribute));

    public static bool HasTransitMethodAttribute(MethodDef methodOwner) => GetTransitMethodAttribute(methodOwner) is not null;

    public static List<MethodDef> GetTransitMethods(ModuleDef module) => module.GetTypes().SelectMany(type => type.Methods).Where(HasTransitMethodAttribute).ToList();
}