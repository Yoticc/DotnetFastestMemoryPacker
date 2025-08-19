using dnlib.DotNet;
using dnlib.DotNet.Emit;

class Patcher
{
    public void Execute(ModuleDefMD module)
    {
        foreach (var type in module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                var body = method.Body;
                if (!body.HasInstructions)
                    continue;

                var locals = body.Variables.Locals;
                var instructions = body.Instructions;
                for (var index = 0; index < instructions.Count; index++)
                {
                    var instruction = instructions[index];
                    if (instruction.IsLdlocOrLdloca())
                    {
                        var nextInstruction = instructions[index + 1];
                        if (nextInstruction.OpCode.Code == Code.Call)
                        {
                            if (nextInstruction.Operand is not MethodSpec calledMethod)
                                continue;
                            
                            if (calledMethod.Name == "Pinnable")
                            {
                                var local = instruction.GetLocal(locals);

                                var oldTypeSig = local.Type;
                                if (!oldTypeSig.IsPinned)
                                {
                                    var newTypeSig = new PinnedSig(oldTypeSig);
                                    local.Type = newTypeSig;
                                    Console.WriteLine($"Set pinned state for local '{local.Name}' for method '{method.Name}'");
                                }

                                Console.WriteLine($"Removed instruction {instructions[index]}");
                                instructions.RemoveAt(index);
                                Console.WriteLine($"Removed instruction {instructions[index]}");
                                instructions.RemoveAt(index);

                                index--;
                            }
                        }
                    }
                }
            }
        }
    }
}