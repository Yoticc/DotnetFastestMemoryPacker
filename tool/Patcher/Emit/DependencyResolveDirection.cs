using dnlib.DotNet.Emit;

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