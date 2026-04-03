using dnlib.DotNet;
using PatcherReference;

partial class Program 
{
    CustomAttribute? GetAttibute(IHasCustomAttribute memberOwner, string name)
    {
        var attributes = memberOwner.CustomAttributes;
        var attribute = attributes.FirstOrDefault(attribute => attribute.AttributeType.Name == name);
        return attribute;
    }

    CustomAttribute? GetUnsafeAccessAttribute(MethodDef methodOwner) => GetAttibute(methodOwner, nameof(UnsafeAccessAttribute));

    bool HasUnsafeAccessAttribute(MethodDef methodOwner) => GetUnsafeAccessAttribute(methodOwner) is not null;

    bool GetUnsafeAccessAttributeArguments(MethodDef methodOwner, out string typeName, out string methodName, out string? methodSignature)
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

    ITypeDefOrRef GetCorlibTypeRef(string @namespace, string name)
    {
        return new Importer(module).Import(GetCorlibTypeDef(@namespace, name));
    }

    TypeDef GetCorlibTypeDef(string @namespace, string name)
    {
        return corlibModule.Types.First(type => type.Namespace == @namespace && type.Name == name);
    }
}