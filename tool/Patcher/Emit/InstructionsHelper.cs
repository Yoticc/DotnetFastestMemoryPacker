using dnlib.DotNet;
using dnlib.DotNet.Emit;

static class InstructionsHelper
{
    public static void ReplaceArgumentWithConstant(IList<Instruction> instructions, Parameter constantArgument, Instruction/*ldc.i4*/ constantValueInstruction)
    {
        if (constantValueInstruction.OpCode.Code != Code.Ldc_I4)
            throw new NotImplementedException("InstructionsHelper.ReplaceArgumentWithConstant does not support instruction other than ldc.i4");

        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (!instruction.IsLdarg())
                continue;

            if (instruction.Operand is not Parameter argument)
                continue;

            if (argument != constantArgument)
                continue;

            Emitter.SetInstruction(instructions, instructionIndex, constantValueInstruction.Clone());
        }
    }

    public static void ReplaceLocalsWithConstants(IList<Instruction> instructions, Local constantLocal, Instruction/*ldc.i4*/ constantValueInstruction)
    {
        if (constantValueInstruction.OpCode.Code != Code.Ldc_I4)
            throw new NotImplementedException("InstructionsHelper.ReplaceArgumentWithConstant does not support instruction other than ldc.i4");

        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (!instruction.IsLdloc())
                continue;

            if (instruction.Operand is not Local local)
                continue;

            if (local != constantLocal)
                continue;

            Emitter.SetInstruction(instructions, instructionIndex, constantValueInstruction.Clone());
        }
    }

    public static void ReplaceLocal(IList<Instruction> instructions, Local fromLocal, Local toLocal)
    {
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (instruction.Operand is not Local local)
                continue;

            if (local != fromLocal)
                continue;
            
            instruction.Operand = toLocal;
        }
    }

    public static void ReplaceLocal(IList<Instruction> instructions, Local fromLocal, Parameter toArgument)
    {
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (instruction.Operand is not Local local)
                continue;

            if (local != fromLocal)
                continue;

            instruction.OpCode = instruction.IsLdloc() ? OpCodes.Ldarg : OpCodes.Starg;
            instruction.Operand = toArgument;
        }
    }

    public static void ReplaceArgument(IList<Instruction> instructions, Parameter fromArgument, Parameter toArgument)
    {
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (instruction.Operand is not Parameter argument)
                continue;

            if (argument != fromArgument)
                continue;

            instruction.Operand = toArgument;
        }
    }

    public static void ReplaceArgument(IList<Instruction> instructions, Parameter fromArgument, Local toLocal)
    {
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (instruction.Operand is not Parameter argument)
                continue;

            if (argument != fromArgument)
                continue;

            instruction.OpCode = instruction.IsLdarg() ? OpCodes.Ldloc : OpCodes.Stloc;
            instruction.Operand = toLocal;
        }
    }

    public static void ReplaceGeneric(IList<Instruction> instructions, GenericParam genericParameter, TypeSig instantiationGenericArgument)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.Operand is not TypeSpec typeSpec)
                continue;

            if (typeSpec.TypeSig is not GenericMVar mvar)
                continue;

            if (mvar.Number != genericParameter.Number)
                continue;

            typeSpec.TypeSig = instantiationGenericArgument;
        }
    }
}