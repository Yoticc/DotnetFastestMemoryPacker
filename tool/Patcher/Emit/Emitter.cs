using dnlib.DotNet;
using dnlib.DotNet.Emit;

// logs every action so now everyone looks and thinks you are a real 1337 badass hacker
static class Emitter
{
    public static void InsertInstructions(IList<Instruction> instructions, int index, IList<Instruction> instructionsToInsert)
    {
        foreach (var instructionToInsert in instructionsToInsert)
            InsertInstruction(instructions, index++, instructionToInsert);
    }

    public static void InsertInstruction(IList<Instruction> instructions, int index, Instruction instruction)
    {
        Console.WriteLine($"Add instruction {instruction.OpCode.Code} after {instructions[index].OpCode.Code}[index: {index}]");
        instructions.Insert(index, instruction);
    }

    public static void SetInstruction(IList<Instruction> instructions, int index, Instruction instructionToSet)
    {
        var oldInstruction = instructions[index];
        Console.WriteLine($"Instruction change {oldInstruction.OpCode.Code} -> {instructionToSet.OpCode.Code}");

        foreach (var instruction in instructions)
        {
            var operand = instruction.Operand;
            if (operand == oldInstruction)
            {
                instruction.Operand = instructionToSet;
                Console.WriteLine($"Instruction dependency update for {instruction.OpCode.Code}");
            }
        }

        instructions[index] = instructionToSet;
    }

    public static void RemoveInstruction(IList<Instruction> instructions, int index, DependencyResolveDirection direction = DependencyResolveDirection.Forward)
    {
        var instructionToRemove = instructions[index];
        Console.WriteLine($"Remove instruction {instructionToRemove.OpCode.Code}");

        if (direction.IsIndexSuitableForResolving(instructions, index))
        {
            var resolvedDependency = direction.GetResolvedDependency(instructions, index);
            for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                var instruction = instructions[instructionIndex];
                var operand = instruction.Operand;
                if (operand == instructionToRemove)
                {
                    instruction.Operand = resolvedDependency;
                    Console.WriteLine($"Instruction dependency update for {instruction.OpCode.Code}, direction: {direction}");
                }
            }
        }

        instructions.RemoveAt(index);
    }

    public static void RemoveType(IList<TypeDef> types, int index)
    {
        Console.WriteLine($"Remove type '{types[index].Name}'");
        types.RemoveAt(index);
    }

    public static void AddType(ModuleDef module, TypeDef type)
    {
        Console.WriteLine($"Add non-nested type '{type.Name}' in module '{module.Name}'");
        module.AddAsNonNestedType(type);
    }

    public static void RemoveAttribute(TypeDef type, CustomAttribute attribute)
    {
        var attributes = type.CustomAttributes;
        Console.WriteLine($"Remove attribute '{attribute.AttributeType.Name}' from '{type.Name}'");
        attributes.Remove(attribute);
    }

    public static void RemoveAttribute(TypeDef type, int index)
    {
        var attributes = type.CustomAttributes;
        Console.WriteLine($"Remove attribute '{attributes[index].AttributeType.Name}' from '{type.Name}'");
        attributes.RemoveAt(index);
    }

    public static void AddAttribute(MethodDef method, CustomAttribute attribute)
    {
        Console.WriteLine($"Add attribute '{attribute.AttributeType.Name}' from '{method.Name}'");
        method.CustomAttributes.Add(attribute);
    }

    public static void RemoveMethod(TypeDef type, int index)
    {
        var methods = type.Methods;
        Console.WriteLine($"Remove method '{methods[index].Name}' from '{type.Name}'");
        methods.RemoveAt(index);
    }

    public static void RemoveMethod(TypeDef type, MethodDef method)
    {
        var methods = type.Methods;
        Console.WriteLine($"Remove method '{method.Name}' from '{type.Name}'");
        methods.Remove(method);
    }

    public static void RemoveMethod(IList<MethodDef> methods, MethodDef method)
    {
        Console.WriteLine($"Remove method '{method.Name}' from {method.DeclaringType.Name}");
        methods.Remove(method);
    }

    public static void RemoveMethod(MethodDef method) => RemoveMethod(method.DeclaringType, method);

    public static void AddMethod(TypeDef type, MethodDef method)
    {
        Console.WriteLine($"Add method '{method.Name}' in '{type.Name}'");
        type.Methods.Add(method);
    }
}