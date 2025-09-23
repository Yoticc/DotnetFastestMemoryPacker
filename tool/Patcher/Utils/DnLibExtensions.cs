using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Security.AccessControl;

static class DnLibExtensions
{
    public static Instruction Clone(this Instruction instruction) => new Instruction
    {
        OpCode = instruction.OpCode,
        Operand = instruction.Operand,
        Offset = instruction.Offset
    };

    public static IList<Instruction> Clone(this IList<Instruction> instructions)
    {
        var copiesInstructions = new List<Instruction>(instructions.Count);

        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            var copiedInstruction = Clone(instruction);
            copiesInstructions.Add(copiedInstruction);
        }

        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var copiedInstruction = copiesInstructions[instructionIndex];
            if (copiedInstruction.Operand is not Instruction operandInstruction)
                continue;

            var offset = operandInstruction.Offset;
            var newOperand = copiesInstructions.FirstOrDefault(instruction => instruction.Offset == offset);
            if (newOperand is null)
                throw new NullReferenceException("DnLibExtensions.Copy: Cannot find the instruction by its old offset.");

            copiedInstruction.Operand = newOperand;
        }

        return copiesInstructions;
    }

    public static bool HasMultipleReturnStatements(this IList<Instruction> instructions)
    {
        var instructionCount = instructions.Count;
        for (var instructionIndex1 = 0; instructionIndex1 < instructionCount;)
        {
            if (instructions[instructionIndex1++].OpCode.Code != Code.Ret)
                continue;

            for (var instructionIndex2 = instructionIndex1 + 1; instructionIndex2 < instructionCount;)
            {
                if (instructions[instructionIndex2++].OpCode.Code == Code.Ret)
                    return true;
            }
        }

        return false;
    }

    public static List<Instruction> GetInstructionsWithThoseOperandInstructions(this IList<Instruction> instructions, IList<Instruction> operandInstructions)
        => instructions.Where(instruction => instruction.Operand is Instruction operand && operandInstructions.Contains(operand)).ToList();

    public static List<Instruction> GetInstructionOutsideDependencies(this IList<Instruction> instructions)
    {
        var outsideInstructions = new List<Instruction>();

        foreach (var instruction in instructions)
        {
            var operand = instruction.Operand;
            if (operand is not Instruction operandInstruction)
                continue;

            if (!instructions.Contains(operandInstruction))
                outsideInstructions.Add(operandInstruction);
        }

        return outsideInstructions;
    }

    public static void SimplifyInstructions(this MethodDef method)
    {
        var body = method.Body;
        body.SimplifyMacros(method.Parameters);
        body.SimplifyBranches();
    }

    public static void OptimizeInstructions(this MethodDef method)
    {
        var body = method.Body;
        body.OptimizeMacros();
        body.OptimizeBranches();
    }

    // extract only MethodDef or MethodSpec->MethodDef
    public static MethodDef? TryExtractMethodDefinition(this Instruction instruction)
    {
        var operand = instruction.Operand;

        if (operand is MethodSpec methodSpec)
        {
            var methodDefOrRef = methodSpec.Method;
            if (!methodDefOrRef.IsMethodDef)
                return null;

            var methodDef = methodSpec.Method as MethodDef;
            return methodDef;
        }
        else if (operand is MethodDef methodDef)
        {
            return methodDef;
        }

        return null;
    }

    public static void TryExtractMethodDefinitionAndInstantiation(this Instruction instruction, out MethodDef? methodDef, out GenericInstMethodSig? instantiation)
    {
        (methodDef, instantiation) = (null, null);

        var operand = instruction.Operand;
        if (operand is MethodSpec methodSpec)
        {
            var methodDefOrRef = methodSpec.Method;
            if (!methodDefOrRef.IsMethodDef)
                return;

            methodDef = methodSpec.Method as MethodDef;
            instantiation = methodSpec.Instantiation as GenericInstMethodSig;
        }
        else if (operand is MethodDef operandMethodDef)
        {
            methodDef = operandMethodDef;
        }

        return;
    }

    public static bool IsConv(this Code code) => code
        is Code.Conv_I
        or Code.Conv_I1
        or Code.Conv_I2
        or Code.Conv_I4
        or Code.Conv_I8
        or Code.Conv_Ovf_I
        or Code.Conv_Ovf_I_Un
        or Code.Conv_Ovf_I1
        or Code.Conv_Ovf_I1_Un
        or Code.Conv_Ovf_I2
        or Code.Conv_Ovf_I2_Un
        or Code.Conv_Ovf_I4
        or Code.Conv_Ovf_I4_Un
        or Code.Conv_Ovf_I8
        or Code.Conv_Ovf_I8_Un
        or Code.Conv_Ovf_U
        or Code.Conv_Ovf_U_Un
        or Code.Conv_Ovf_U1
        or Code.Conv_Ovf_U1_Un
        or Code.Conv_Ovf_U2
        or Code.Conv_Ovf_U2_Un  
        or Code.Conv_Ovf_U4
        or Code.Conv_Ovf_U4_Un
        or Code.Conv_Ovf_U8
        or Code.Conv_Ovf_U8_Un
        or Code.Conv_U
        or Code.Conv_U1
        or Code.Conv_U2
        or Code.Conv_U4
        or Code.Conv_U8
        or Code.Conv_R4
        or Code.Conv_R8;

    public static bool IsCompare(this Code code) => code
        is Code.Ceq
        or Code.Clt
        or Code.Cgt;

    public static bool IsLdcI4(this Code code) => code
        is Code.Ldc_I4
        or Code.Ldc_I4_0
        or Code.Ldc_I4_1
        or Code.Ldc_I4_2
        or Code.Ldc_I4_3
        or Code.Ldc_I4_4
        or Code.Ldc_I4_5
        or Code.Ldc_I4_6
        or Code.Ldc_I4_7
        or Code.Ldc_I4_8
        or Code.Ldc_I4_M1
        or Code.Ldc_I4_S
        or Code.Ldc_I4;

    public static bool IsBge(this Code code) => code is Code.Bge or Code.Bge_S or Code.Bge_Un or Code.Bge_Un_S;

    public static bool IsBlt(this Code code) => code is Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S;

    public static bool IsBgt(this Code code) => code is Code.Bgt or Code.Bgt_S or Code.Bgt_Un or Code.Bgt_Un_S;

    public static bool IsBle(this Code code) => code is Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S;

    public static bool IsBrFalse(this Code code) => code is Code.Brfalse or Code.Brfalse_S;

    public static bool IsBrTrue(this Code code) => code is Code.Brtrue or Code.Brtrue_S;

    public static bool IsBeq(this Code code) => code is Code.Beq or Code.Beq_S;

    public static bool IsBne(this Code code) => code is Code.Bne_Un or Code.Bne_Un_S;

    public static bool IsConditionalBranch(this Code code) => 
        code.IsBge() || 
        code.IsBlt() || 
        code.IsBgt() || 
        code.IsBle() || 
        code.IsBrFalse() || 
        code.IsBrTrue() || 
        code.IsBeq() ||
        code.IsBne();

    public static bool IsBr(this Code code) => code is Code.Br or Code.Br_S;

    public static bool IsLdloc(this Code code) => 
        code is
        Code.Ldloc_0 or
        Code.Ldloc_1 or
        Code.Ldloc_2 or
        Code.Ldloc_3 or
        Code.Ldloc_S or
        Code.Ldloc;

    public static bool IsLdloca(this Code code) => code is Code.Ldloca_S or Code.Ldloca;

    public static bool IsLdlocOrLdloca(this Code code) => code.IsLdloc() || code.IsLdloca();

    public static bool IsLdlocOrLdloca(this Instruction instruction) => instruction.OpCode.Code.IsLdlocOrLdloca();

    public static bool IsLdarg(this Code code) =>
        code is
        Code.Ldarg_0 or
        Code.Ldarg_1 or
        Code.Ldarg_2 or
        Code.Ldarg_3 or
        Code.Ldarg_S or
        Code.Ldarg;

    public static bool IsLdarga(this Code code) => code is Code.Ldarga_S or Code.Ldarga;

    public static bool IsLdargOrLdarga(this Code code) => code.IsLdarg() || code.IsLdarga();

    public static bool IsLdargOrLdarga(this Instruction instruction) => instruction.OpCode.Code.IsLdargOrLdarga();

    public static bool IsStloc(this Code code) =>
        code is
        Code.Stloc_0 or
        Code.Stloc_1 or
        Code.Stloc_2 or
        Code.Stloc_3 or
        Code.Stloc_S or
        Code.Stloc;

    public static bool IsStarg(this Code code) => code is Code.Starg_S or Code.Starg;

    public static bool IsArgumentRelated(this Code code) => code.IsLdargOrLdarga() || code.IsStarg();

    public static bool IsLocalRelated(this Code code) => code.IsStarg() || code.IsStloc();

    public static bool IsNop(this Instruction instruction) => instruction.OpCode.Code is Code.Nop;

    public static Local GetLocalOperand(this Instruction instruction)
    {
        if (instruction.Operand is not Local operandLocal)
            throw new Exception($"Instruction.GetLocalOperand: {instruction.OpCode.Code} is not suitable for this method.");
        return operandLocal;
    }

    public static Parameter GetArgumentOperand(this Instruction instruction)
    {
        if (instruction.Operand is not Parameter operandParameter)
            throw new Exception($"Instruction.GetArgumentOperand: {instruction.OpCode.Code} is not suitable for this method.");
        return operandParameter;
    }

    public static int GetLdlocOperand(this Instruction instruction) => instruction.OpCode.Code switch
    {
        Code.Ldloc_0 => 0,
        Code.Ldloc_1 => 1,
        Code.Ldloc_2 => 2,
        Code.Ldloc_3 => 3,
        Code.Ldloc_S or Code.Ldloc => ((Local)instruction.Operand).Index,
        _ => throw new Exception()
    };

    public static int GetLdlocaOperand(this Instruction instruction) => instruction.OpCode.Code switch
    {
        Code.Ldloca_S or Code.Ldloca => ((Local)instruction.Operand).Index,
        _ => throw new Exception()
    };

    public static int GetLdargOperand(this Instruction instruction) => instruction.OpCode.Code switch
    {
        Code.Ldarg_0 => 0,
        Code.Ldarg_1 => 1,
        Code.Ldarg_2 => 2,
        Code.Ldarg_3 => 3,
        Code.Ldarg_S or Code.Ldarg => ((Parameter)instruction.Operand).Index,
        _ => throw new Exception()
    };

    public static int GetLdargaOperand(this Instruction instruction) => instruction.OpCode.Code switch
    {
        Code.Ldarga_S or Code.Ldarga => ((Parameter)instruction.Operand).Index,
        _ => throw new Exception()
    };

    public static int GetStlocOperand(this Instruction instruction) => instruction.OpCode.Code switch
    {
        Code.Stloc_0 => 0,
        Code.Stloc_1 => 1,
        Code.Stloc_2 => 2,
        Code.Stloc_3 => 3,
        Code.Stloc_S or Code.Stloc => ((Local)instruction.Operand).Index,
        _ => throw new Exception()
    };

    public static int GetStargOperand(this Instruction instruction) => instruction.OpCode.Code switch
    {
        Code.Starg_S or Code.Starg => ((Parameter)instruction.Operand).Index,
        _ => throw new Exception()
    };

    public static List<Parameter> GetArguments(this MethodDef method) => method.Parameters.Where(param => !param.IsReturnTypeParameter).ToList();

    public static Parameter? GetReturnParameter(this MethodDef method)
    {
        var parameter = method.Parameters.FirstOrDefault(param => param.IsReturnTypeParameter);
        if (parameter is null || parameter.Type.GetName() == "Void")
            return null;
        return parameter;
    }
}