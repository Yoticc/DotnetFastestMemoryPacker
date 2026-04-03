using dnlib.DotNet;
using dnlib.DotNet.Emit;

partial class Program
{
    TypeDefUser DefineType(string @namespace, string name, ITypeDefOrRef baseType)
    {
        var fullname = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

        TypeDefUser definedType = null!;
        ExecuteTask($"Define type '{fullname}'", () =>
        {
            definedType = new TypeDefUser(@namespace, name, baseType);
            module.AddAsNonNestedType(definedType);
        });

        return definedType;
    }

    MethodDefUser DefineMethod(TypeDef declaringType, TypeSig returnTypeSig, TypeSig[] argsSig, MethodAttributes attributes, string name)
    {
        var methodSignature = MethodSig.CreateInstance(returnTypeSig, argsSig);
        return DefineMethod(declaringType, methodSignature, attributes, name);
    }

    MethodDefUser DefineMethod(TypeDef declaringType, MethodSig signature, MethodAttributes attributes, string name)
    {
        MethodDefUser definedMethod = null!;
        ExecuteTask($"Define empty constructor for type '{declaringType.Name}'", () =>
        {
            var ctorSignature = MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String);
            var definedMethod = new MethodDefUser(name, signature, MethodImplAttributes.IL, attributes);
            definedMethod.Body = new CilBody();

            declaringType.Methods.Add(definedMethod);
        });

        return definedMethod;
    }

    void ReplaceType(ModuleDef module, TypeDef replaceType, ITypeDefOrRef withType)
    {
        ExecuteTask($"Replace type {replaceType.Name} with {withType.Name}", () =>
        {
            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    var parameters = method.Parameters;
                    for (var parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
                    {
                        var parameter = parameters[parameterIndex];
                        var typeSig = parameter.Type;
                        if (typeSig is TypeDefOrRefSig typeDefSig)
                        {
                            var typeSigDef = typeDefSig.TypeDefOrRef;
                            if (typeSigDef == replaceType)
                                parameter.Type = withType.ToTypeSig();
                        }
                    }
                }

                foreach (var field in type.Fields)
                {
                    var typeSig = field.FieldType;
                    if (typeSig is TypeDefOrRefSig typeDefSig)
                    {
                        var typeSigDef = typeDefSig.TypeDefOrRef;
                        if (typeSigDef == replaceType)
                            field.FieldType = withType.ToTypeSig();
                    }
                }
            }
        });
    }
}