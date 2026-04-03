using dnlib.DotNet;
using dnlib.DotNet.Emit;

partial class Program
{
    void InsertInstructions(IList<Instruction> instructions, int index, IList<Instruction> instructionsToInsert)
    {
        foreach (var instructionToInsert in instructionsToInsert)
            InsertInstruction(instructions, index++, instructionToInsert);
    }

    void InsertInstruction(IList<Instruction> instructions, int index, Instruction instruction)
    {
        PrintComment($"Add instruction {instruction.OpCode.Code} after {instructions[index].OpCode.Code}[index: {index}]");
        instructions.Insert(index, instruction);
    }

    void SetInstruction(IList<Instruction> instructions, int index, Instruction instructionToSet)
    {
        var oldInstruction = instructions[index];
        PrintComment($"Instruction change {oldInstruction.OpCode.Code} -> {instructionToSet.OpCode.Code}");

        foreach (var instruction in instructions)
        {
            var operand = instruction.Operand;
            if (operand == oldInstruction)
            {
                instruction.Operand = instructionToSet;
                PrintComment($"Instruction dependency update for {instruction.OpCode.Code}");
            }
        }

        instructions[index] = instructionToSet;
    }

    void ResolveDependency(IList<Instruction> instructions, Instruction oldDependency, Instruction newDependency)
    {
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            var operand = instruction.Operand;

            if (operand is Instruction)
            {
                if (operand == oldDependency)
                {
                    instruction.Operand = newDependency;
                    PrintComment($"Instruction dependency update for {instruction.OpCode.Code}");
                }
            }
            else if (operand is Instruction[] operandInstructions)
            {
                var caseCount = operandInstructions.Length;
                for (var caseIndex = 0; caseIndex < caseCount; caseIndex++)
                {
                    var operandInstruction = operandInstructions[caseIndex];
                    if (operandInstruction == oldDependency)
                    {
                        operandInstructions[caseIndex] = newDependency;
                        PrintComment($"Instruction dependency update for {instruction.OpCode.Code}");
                    }
                }
            }
        }
    }

    void RemoveInstructionWithDependencyReplacement(IList<Instruction> instructions, int index, Instruction resolveDependenciesWith)
    {
        var instructionToRemove = instructions[index];
        ResolveDependency(instructions, instructionToRemove, resolveDependenciesWith);

        PrintComment($"Remove instruction {instructionToRemove.OpCode.Code}");
        instructions.RemoveAt(index);
    }

    void RemoveInstruction(IList<Instruction> instructions, int index, DependencyResolveDirection direction = DependencyResolveDirection.Forward)
    {
        var instructionToRemove = instructions[index];

        if (direction.IsIndexSuitableForResolving(instructions, index))
        {
            var resolvedDependency = direction.GetResolvedDependency(instructions, index);
            ResolveDependency(instructions, instructionToRemove, resolvedDependency);
        }

        PrintComment($"Remove instruction {instructionToRemove.OpCode.Code}");
        instructions.RemoveAt(index);
    }

    void RemoveType(IList<TypeDef> types, int index)
    {
        PrintComment($"Remove type '{types[index].Name}'");
        types.RemoveAt(index);
    }

    void RemoveType(IList<TypeDef> types, TypeDef type)
    {
        PrintComment($"Remove type '{type.Name}'");
        types.Remove(type);
    }

    void AddType(ModuleDef module, TypeDef type)
    {
        PrintComment($"Add non-nested type '{type.Name}' in module '{module.Name}'");
        module.AddAsNonNestedType(type);
    }

    void RemoveAttribute(TypeDef type, CustomAttribute attribute)
    {
        var attributes = type.CustomAttributes;
        PrintComment($"Remove attribute '{attribute.AttributeType.Name}' from '{type.Name}'");
        attributes.Remove(attribute);
    }

    void RemoveAttribute(TypeDef type, int index)
    {
        var attributes = type.CustomAttributes;
        PrintComment($"Remove attribute '{attributes[index].AttributeType.Name}' from '{type.Name}'");
        attributes.RemoveAt(index);
    }

    void AddAttribute(MethodDef method, CustomAttribute attribute)
    {
        PrintComment($"Add attribute '{attribute.AttributeType.Name}' from '{method.Name}'");
        method.CustomAttributes.Add(attribute);
    }

    void RemoveMethod(TypeDef type, int index)
    {
        var methods = type.Methods;
        PrintComment($"Remove method '{methods[index].Name}' from '{type.Name}'");
        methods.RemoveAt(index);
    }

    void RemoveMethod(TypeDef type, MethodDef method)
    {
        var methods = type.Methods;
        PrintComment($"Remove method '{method.Name}' from '{type.Name}'");
        methods.Remove(method);
    }

    void RemoveMethod(IList<MethodDef> methods, MethodDef method)
    {
        PrintComment($"Remove method '{method.Name}' from {method.DeclaringType.Name}");
        methods.Remove(method);
    }

    void RemoveMethod(MethodDef method) => RemoveMethod(method.DeclaringType, method);

    void AddMethod(TypeDef type, MethodDef method)
    {
        PrintComment($"Add method '{method.Name}' in '{type.Name}'");
        type.Methods.Add(method);
    }
}

enum DependencyResolveDirection
{
    Forward,
    Backward
}

static class DependencyResolveDirectionExtensions
{
    public static bool IsIndexSuitableForResolving(this DependencyResolveDirection self, IList<Instruction> instructions, int index) =>
        self switch
        {
            DependencyResolveDirection.Forward => index + 1 < instructions.Count,
            DependencyResolveDirection.Backward => index - 1 > -1,
            _ => throw new NotImplementedException()
        };

    public static Instruction GetResolvedDependency(this DependencyResolveDirection self, IList<Instruction> instructions, int index) =>
        self switch
        {
            DependencyResolveDirection.Forward => instructions[index + 1],
            DependencyResolveDirection.Backward => instructions[index - 1],
            _ => throw new NotImplementedException()
        };
}