using dnlib.DotNet;
using dnlib.DotNet.Emit;
using PatcherReference;

class Patcher
{
    public void Execute(ModuleDefMD module)
    {
        HandleAttributes(module);
        foreach (var type in module.GetTypes())
            foreach (var method in type.Methods)
                HandleExtrinsics(method);
    }

    void HandleAttributes(ModuleDefMD module)
    {
        var types = module.Types;
        for (var typeIndex = 0; typeIndex < types.Count; typeIndex++)
        {
            var type = types[typeIndex];
            var attributes = type.CustomAttributes;
            for (var attributeIndex = 0; attributeIndex < attributes.Count; attributeIndex++)
            {
                var attribute = attributes[attributeIndex];
                switch (attribute.AttributeType.Name)
                {
                    case nameof(ShouldBeTrimmedAttribute):
                        {
                            RemoveType(module.Types, typeIndex--);
                            break;
                        }
                    case nameof(InlineAllMembersAttribute):
                        {
                            foreach (var method in type.Methods)
                                method.ImplAttributes |= MethodImplAttributes.AggressiveInlining;

                            RemoveAttribute(type, attributes, attributeIndex);
                            break;
                        }
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

            if (instruction.OpCode.Code != Code.Call)
                continue;

            if (instruction.Operand is not IMethod calledMethod)
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

                            var localName = local.Name;
                            if (string.IsNullOrEmpty(localName))
                                localName = $"V_{local.Index}";

                            Console.WriteLine($"Set pinned state for local '{localName}'");
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
}