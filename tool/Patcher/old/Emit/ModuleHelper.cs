using dnlib.DotNet;

static class ModuleHelper
{
    // replaces one type definition with another. does not support generic signatures
    public static void ReplaceType(ModuleDef module, TypeDef replaceType, ITypeDefOrRef withType)
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
    }
}