using dnlib.DotNet;
using dnlib.DotNet.Emit;
using PatcherReference;

class Patcher
{
    public void Execute(ModuleDefMD corlibModule, ModuleDefMD module)
    {
        HandleUnsafeAccessors(corlibModule, module);
        HandleInlineAllMembers(module);
        foreach (var type in module.GetTypes())
            foreach (var method in type.Methods)
                HandleExtrinsics(method);

        TransitInliner.Execute(module);
        HandleShouldBeTrimmed(module);
    }

    void HandleUnsafeAccessors(ModuleDefMD corlibModule, ModuleDefMD module)
    {
        DefineIgnoreAccessChecksToAttribute(corlibModule, module);
        foreach (var type in module.Types)
        {
            var methods = type.Methods;
            for (var methodIndex = 0; methodIndex < methods.Count; methodIndex++)
            {
                var method = methods[methodIndex];
                if (!method.HasBody)
                    continue;

                var body = method.Body;
                var instructions = body.Instructions;
                for (var index = 0; index < instructions.Count; index++)
                {
                    var instruction = instructions[index];

                    if (instruction.OpCode.Code is not Code.Call)
                        continue;

                    if (instruction.Operand is not MethodDef calledMethod)
                        continue;

                    var unsafeAccessAttribute = calledMethod.CustomAttributes.FirstOrDefault(attribute => attribute.AttributeType.Name == "UnsafeAccessAttribute");

                    if (unsafeAccessAttribute is null)
                        continue;

                    var args = unsafeAccessAttribute.ConstructorArguments;
                    var typeName = args[0].Value as UTF8String;
                    if (typeName is null)
                        throw new Exception("HandleUnsafeAccessors: 1-th arg, type name, was null");

                    var methodName = args[1].Value as UTF8String;
                    if (methodName is null)
                        methodName = calledMethod.Name;

                    var methodSignature = (UTF8String)args[2].Value;
                    var declaringType = corlibModule.Find(typeName, false);

                    MethodDef? attributedMethod;
                    if (methodSignature is not null)
                        attributedMethod = declaringType.Methods.FirstOrDefault(m => m.Name == methodName && m.ToString() == methodSignature);
                    else attributedMethod = declaringType.Methods.FirstOrDefault(m => m.Name == methodName);

                    if (attributedMethod is null)
                        throw new Exception($"HandleUnsafeAccessors: can not find a method with name '{methodName}' in type '{typeName}'");

                    if (calledMethod.Parameters.Count != attributedMethod.Parameters.Count)
                    {
                        var availableSignatures = string.Join('\n', declaringType.Methods.Where(m => m.Name == methodName).Select(m => $"'{m}'"));
                        throw new Exception(
                            $"HandleUnsafeAccessors: miscounting of arguments for method '{methodName} in type '{typeName}'.\n" + 
                            $"Available signatures: {availableSignatures}"
                        );
                    }

                    var importer = new Importer(module);
                    var importedMethod = importer.Import(attributedMethod);
                    if (importedMethod is null)
                        throw new Exception($"HandleUnsafeAccessors: can not import found method with name '{methodName}' in type '{typeName}'");

                    instruction.Operand = importedMethod;
                    Console.WriteLine($"Rewrite implementation for method '{method.Name}'");
                }
            }
        }

        foreach (var type in module.Types)
        {
            var methods = type.Methods;
            for (var methodIndex = 0; methodIndex < methods.Count;)
            {
                var method = methods[methodIndex];
                var hasUnsafeAccessAttribute = method.CustomAttributes.Any(attribute => attribute.AttributeType.Name == "UnsafeAccessAttribute");
                if (hasUnsafeAccessAttribute)
                {
                    RemoveMethod(type, methods, methodIndex);
                    continue;
                }

                methodIndex++;
            }
        }

        static void DefineIgnoreAccessChecksToAttribute(ModuleDefMD corlibModule, ModuleDefMD module)
        {
            var importer = new Importer(module);
            var attributeType = corlibModule.Find("System.Attribute", false);
            var userType = new TypeDefUser(@namespace: "System.Runtime.CompilerServices", name: "IgnoresAccessChecksToAttribute", baseType: importer.Import(attributeType));
            AddType(module, userType);

            var ctorSignature = MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String);
            var ctorAttributes = MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            var constructor = new MethodDefUser(".ctor", ctorSignature, MethodImplAttributes.IL, ctorAttributes);
            var body = constructor.Body = new CilBody();
            var instructions = body.Instructions;
            instructions.Add(new Instruction(OpCodes.Ldarg_0));
            instructions.Add(new Instruction(OpCodes.Call, importer.Import(attributeType.FindDefaultConstructor())));
            instructions.Add(new Instruction(OpCodes.Ret));
            AddMethod(userType, constructor);

            var accessToLibrary = "System.Private.CoreLib";
            var moduleAttribute = new CustomAttribute(constructor, (CAArgument[])[new CAArgument(module.CorLibTypes.String, (UTF8String)accessToLibrary)]);
            module.Assembly.CustomAttributes.Add(moduleAttribute);
        }
    }

    void HandleShouldBeTrimmed(ModuleDefMD module)
    {
        var types = module.Types;
        for (var typeIndex = 0; typeIndex < types.Count; typeIndex++)
        {
            var type = types[typeIndex];
            var attributes = type.CustomAttributes;
            for (var attributeIndex = 0; attributeIndex < attributes.Count; attributeIndex++)
            {
                var attribute = attributes[attributeIndex];
                if (attribute.AttributeType.Name == nameof(ShouldBeTrimmedAttribute))
                {
                    RemoveType(module.Types, typeIndex--);
                    break;
                }
            }
        }
    }

    void HandleInlineAllMembers(ModuleDefMD module)
    {
        var types = module.Types;
        for (var typeIndex = 0; typeIndex < types.Count; typeIndex++)
        {
            var type = types[typeIndex];
            var attributes = type.CustomAttributes;
            for (var attributeIndex = 0; attributeIndex < attributes.Count; attributeIndex++)
            {
                var attribute = attributes[attributeIndex];
                if (attribute.AttributeType.Name == nameof(InlineAllMembersAttribute))
                {
                    foreach (var method in type.Methods)
                        method.ImplAttributes |= MethodImplAttributes.AggressiveInlining;

                    RemoveAttribute(type, attributes, attributeIndex);
                    break;
                }
            }
        }
    }

    void HandleExtrinsics(MethodDef method)
    {
        if (!method.HasBody)
            return;

        var body = method.Body;
        var instructions = body.Instructions;

        for (var index = 0; index < instructions.Count; index++)
        {
            var instruction = instructions[index];

            if (instruction.OpCode.Code is not Code.Call)
                continue;

            if (instruction.Operand is not IMethod calledMethod)
                continue;

            if (calledMethod.DeclaringType is null)
                continue;

            if (calledMethod.DeclaringType.Name != nameof(Extrinsics))
                continue;

            var methodName = calledMethod.Name;
            switch (methodName)
            {
                case nameof(Extrinsics.Pinnable):
                    {
                        if (index < 1)
                            throw new Exception("HandleExtrinsics: [Pinnable] impossible call: no arguments");

                        var prevInstruction = instructions[index - 1];
                        if (!prevInstruction.IsLdlocOrLdloca())
                            throw new Exception("HandleExtrinsics: [Pinnable] passed variable is not a local");

                        var local = prevInstruction.GetLocal(body.Variables.Locals);

                        var oldTypeSig = local.Type;
                        if (!oldTypeSig.IsPinned)
                        {
                            var newTypeSig = new PinnedSig(oldTypeSig);
                            local.Type = newTypeSig;

                            Console.WriteLine($"Set pinned state for local '{local}'");
                        }

                        RemoveInstruction(instructions, --index);
                        RemoveInstruction(instructions, index);
                        break;
                    }
                case nameof(Extrinsics.GetTypeHandle):
                    {
                        if (calledMethod is not MethodSpec calledMethodSpec)
                            throw new Exception("HandleExtrinsics: [GetTypeHandle] the method is not generic");

                        var genericSignature = calledMethodSpec.GenericInstMethodSig;
                        var genericArgument = genericSignature.GenericArguments.First();
                        var typeSpec = new TypeSpecUser(genericArgument);

                        SetInstruction(instructions, index, OpCodes.Ldtoken.ToInstruction(typeSpec));
                        break;
                    }
                case nameof(Extrinsics.LoadEffectiveAddress):
                    {
                        SetInstruction(instructions, index, new Instruction(OpCodes.Add));
                        break;
                    }
                case nameof(Extrinsics.As):
                    {
                        RemoveInstruction(instructions, index--);
                        break;
                    }
            }
        }
    }

    static void SetInstruction(IList<Instruction> instructions, int index, Instruction instruction)
    {
        Console.WriteLine($"Set instruction '{instruction.OpCode.Code}' instead of instruction '{instructions[index].OpCode.Code}'");
        instructions[index] = instruction;
    }

    static void RemoveInstruction(IList<Instruction> instructions, int index)
    {
        Console.WriteLine($"Remove instruction '{instructions[index].OpCode.Code}'");
        instructions.RemoveAt(index);
    }

    static void RemoveType(IList<TypeDef> types, int index)
    {
        Console.WriteLine($"Remove type '{types[index].Name}'");
        types.RemoveAt(index);
    }

    static void RemoveAttribute(TypeDef type, CustomAttributeCollection attributes, int index)
    {
        Console.WriteLine($"Remove attribute '{attributes[index].AttributeType.Name}' from '{type.Name}'");
        attributes.RemoveAt(index);
    }

    static void RemoveMethod(TypeDef type, IList<MethodDef> methods, int index)
    {
        Console.WriteLine($"Remove method '{methods[index].Name}' from '{type.Name}'");
        methods.RemoveAt(index);
    }

    static void AddAttribute(MethodDef method, CustomAttribute attribute)
    {
        Console.WriteLine($"Add attribute '{attribute.AttributeType.Name}' from '{method.Name}'");
        method.CustomAttributes.Add(attribute);
    }

    static void AddType(ModuleDef module, TypeDef type)
    {
        Console.WriteLine($"Add non-nested type '{type.Name}' in module '{module.Name}'");
        module.AddAsNonNestedType(type);
    }

    static void AddMethod(TypeDef type, MethodDef method)
    {
        Console.WriteLine($"Add method '{method.Name}' in '{type.Name}'");
        type.Methods.Add(method);
    }
}