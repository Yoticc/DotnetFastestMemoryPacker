using dnlib.DotNet.Emit;

static class DnLibExtensions
{
    public static bool IsLdlocOrLdloca(this Instruction instruction) =>
        instruction.OpCode.Code
        is Code.Ldloc
        or Code.Ldloc_S
        or Code.Ldloca
        or Code.Ldloca_S
        or Code.Ldloc_0
        or Code.Ldloc_1
        or Code.Ldloc_2
        or Code.Ldloc_3;

    public static bool IsNop(this Instruction instruction) => instruction.OpCode.Code is Code.Nop;
}