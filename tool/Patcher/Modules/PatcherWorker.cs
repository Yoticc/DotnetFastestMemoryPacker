using dnlib.DotNet;
using dnlib.DotNet.Emit;
using PatcherReference;

static class PatcherWorker
{
    public static void Execute(ModuleDefMD corlibModule, ModuleDefMD module)
    {
        HandleUnsafeAccessors(corlibModule, module);
        foreach (var type in module.GetTypes())
            foreach (var method in type.Methods)
                HandleExtrinsics(method);
    }

    static void HandleUnsafeAccessors(ModuleDefMD corlibModule, ModuleDefMD module)
    {
        if (module.TypeExists("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", false))
            return;

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

                    if (!PatcherProvider.GetUnsafeAccessAttributeArguments(calledMethod, out var typeName, out var methodName, out var methodSignature))
                        continue;

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
                if (PatcherProvider.HasUnsafeAccessAttribute(method))
                    Emitter.RemoveMethod(type, methodIndex);
                else methodIndex++;
            }
        }

        static void DefineIgnoreAccessChecksToAttribute(ModuleDefMD corlibModule, ModuleDefMD module)
        {
            var importer = new Importer(module);
            var attributeType = corlibModule.Find("System.Attribute", false);
            var userType = new TypeDefUser(@namespace: "System.Runtime.CompilerServices", name: "IgnoresAccessChecksToAttribute", baseType: importer.Import(attributeType));
            Emitter.AddType(module, userType);

            var ctorSignature = MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String);
            var ctorAttributes = MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            var constructor = new MethodDefUser(".ctor", ctorSignature, MethodImplAttributes.IL, ctorAttributes);
            var body = constructor.Body = new CilBody();
            var instructions = body.Instructions;
            instructions.Add(new Instruction(OpCodes.Ldarg_0));
            instructions.Add(new Instruction(OpCodes.Call, importer.Import(attributeType.FindDefaultConstructor())));
            instructions.Add(new Instruction(OpCodes.Ret));
            Emitter.AddMethod(userType, constructor);

            var accessToLibrary = "System.Private.CoreLib";
            var moduleAttribute = new CustomAttribute(constructor, (CAArgument[])[new CAArgument(module.CorLibTypes.String, (UTF8String)accessToLibrary)]);
            module.Assembly.CustomAttributes.Add(moduleAttribute);
        }
    }

    static void HandleExtrinsics(MethodDef method)
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

                        Emitter.RemoveInstruction(instructions, --index);
                        Emitter.RemoveInstruction(instructions, index);
                        break;
                    }
                case nameof(Extrinsics.Uninitialized):
                    {
                        Emitter.RemoveInstruction(instructions, index--); // call
                        Emitter.RemoveInstruction(instructions, index--); // ldloc
                        break;
                    }
                case nameof(Extrinsics.GetTypeHandle):
                    {
                        if (calledMethod is not MethodSpec calledMethodSpec)
                            throw new Exception("HandleExtrinsics: [GetTypeHandle] the method is not generic");

                        var genericSignature = calledMethodSpec.GenericInstMethodSig;
                        var genericArgument = genericSignature.GenericArguments.First();
                        var typeSpec = new TypeSpecUser(genericArgument);

                        Emitter.SetInstruction(instructions, index, OpCodes.Ldtoken.ToInstruction(typeSpec));
                        break;
                    }
                case nameof(Extrinsics.LoadEffectiveAddress):
                    {
                        Emitter.SetInstruction(instructions, index, new Instruction(OpCodes.Add)); // call -> add
                        break;
                    }
                case nameof(Extrinsics.As):
                    {
                        Emitter.RemoveInstruction(instructions, index--); // call
                        break;
                    }
            }
        }
    }
}