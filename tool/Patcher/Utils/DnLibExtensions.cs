using dnlib.DotNet;
using dnlib.DotNet.Emit;

static class DnLibExtensions
{
    public static bool IsLdloc(this Code code) => 
        code is
        Code.Ldloc_0 or
        Code.Ldloc_1 or
        Code.Ldloc_2 or
        Code.Ldloc_3 or
        Code.Ldloc_S or
        Code.Ldloc;

    public static bool IsLdloca(this Code code) =>
        code is
        Code.Ldloca_S or
        Code.Ldloca;

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

    public static bool IsLdarga(this Code code) =>
        code is
        Code.Ldarga_S or
        Code.Ldarga;

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

    public static bool IsStarg(this Code code) =>
        code is
        Code.Starg_S or
        Code.Starg;

    public static bool IsArgumentRelated(this Code code) => code.IsLdargOrLdarga() || code.IsStarg();

    public static bool IsLocalRelated(this Code code) => code.IsStarg() || code.IsStloc();

    public static bool IsNop(this Instruction instruction) => instruction.OpCode.Code is Code.Nop;

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

    public static Parameter[] GetArguments(this MethodDef method) => method.Parameters.Where(param => !param.IsReturnTypeParameter).ToArray();

    public static Parameter? GetReturnParameter(this MethodDef method)
    {
        var parameter = method.Parameters.FirstOrDefault(param => param.IsReturnTypeParameter);
        if (parameter is null || parameter.Type.GetName() == "Void")
            return null;
        return parameter;
    }
}