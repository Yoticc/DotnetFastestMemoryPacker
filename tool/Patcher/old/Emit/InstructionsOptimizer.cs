using dnlib.DotNet.Emit;

static class InstructionsOptimizer
{
    public static void OptimizeConditionsAndBranches(IList<Instruction> instructions)
    {
        OptimizeComparisonForConstants(instructions);
        OptimizeConditionalBranches(instructions);
    }

    static void OptimizeComparisonForConstants(IList<Instruction> instructions)
    {
        for (var instructionIndex = 2; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            var opCode = instruction.OpCode.Code;

            if (!opCode.IsCompare())
                continue;

            var p1Instruction = instructions[instructionIndex - 1];
            var p2Instruction = instructions[instructionIndex - 2];
            if (!p1Instruction.IsLdcI4() || !p2Instruction.IsLdcI4())
                continue;

            var p1Operand = p1Instruction.GetLdcI4Value();
            var p2Operand = p2Instruction.GetLdcI4Value();

            var condition =
                (opCode == Code.Ceq && p1Operand == p2Operand) ||
                (opCode == Code.Cgt && p1Operand > p2Operand) ||
                (opCode == Code.Clt && p1Operand < p2Operand);

            var value = condition ? 1 : 0;
            instruction = new Instruction(OpCodes.Ldc_I4, value);
            Emitter.SetInstruction(instructions, instructionIndex, instruction);

            Emitter.RemoveInstruction(instructions, instructionIndex - 1);
            Emitter.RemoveInstruction(instructions, instructionIndex - 2);

            instructionIndex -= 2;
        }
    }

    public static void OptimizeConditionalBranches(IList<Instruction> instructions)
    {
        for (var instructionIndex = 2; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];

            var opCode = instruction.OpCode.Code;
            if (!opCode.IsConditionalBranch() || opCode.IsBrTrue() || opCode.IsBrFalse())
                continue;

            var p1Instruction = instructions[instructionIndex - 1];
            var p2Instruction = instructions[instructionIndex - 2];
            if (!p1Instruction.IsLdcI4() || !p2Instruction.IsLdcI4())
                continue;

            var p1Operand = p1Instruction.GetLdcI4Value();
            var p2Operand = p2Instruction.GetLdcI4Value();

            var conditionState = 
                (opCode.IsBge() && p2Operand >= p1Operand) ||
                (opCode.IsBlt() && p2Operand < p1Operand) ||
                (opCode.IsBgt() && p2Operand > p1Operand) ||
                (opCode.IsBle() && p2Operand <= p1Operand) ||
                (opCode.IsBeq() && p2Operand == p1Operand) ||
                (opCode.IsBne() && p2Operand != p1Operand);

            var branchTagetInstruction = instruction.Operand;
            if (conditionState)
            {
                instructionIndex -= 2;

                while (instructionIndex < instructions.Count && instructions[instructionIndex] != branchTagetInstruction)
                    Emitter.RemoveInstruction(instructions, instructionIndex);

                instructionIndex--;
            }
            else 
            {
                Emitter.RemoveInstruction(instructions, instructionIndex);
                Emitter.RemoveInstruction(instructions, instructionIndex - 1);
                Emitter.RemoveInstruction(instructions, instructionIndex - 2);
                instructionIndex -= 3;
            }
        }

        for (var instructionIndex = 1; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];

            var opCode = instruction.OpCode.Code;
            if (!opCode.IsBrTrue() && !opCode.IsBrFalse())
                continue;

            var pInstruction = instructions[instructionIndex - 1];
            if (!pInstruction.IsLdcI4())
                continue;

            var pOperand = pInstruction.GetLdcI4Value();

            var conditionState =
                (opCode.IsBrTrue() && pOperand != 0) ||
                (opCode.IsBrFalse() && pOperand == 0);

            var branchTagetInstruction = instruction.Operand;
            if (conditionState)
            {
                instructionIndex -= 1;

                while (instructionIndex < instructions.Count && instructions[instructionIndex] != branchTagetInstruction)
                    Emitter.RemoveInstruction(instructions, instructionIndex);

                instructionIndex--;
            }
            else
            {
                Emitter.RemoveInstruction(instructions, instructionIndex);
                Emitter.RemoveInstruction(instructions, instructionIndex - 1);
                instructionIndex -= 2;
            }
        }
    }
}