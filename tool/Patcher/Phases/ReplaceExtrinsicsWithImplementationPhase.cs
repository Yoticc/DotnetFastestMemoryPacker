using dnlib.DotNet;
using dnlib.DotNet.Emit;
using PatcherReference;

partial class Program
{
    void ReplaceExtrinsicsWithImplementationPhase()
    {
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                var body = method.Body;
                var instructions = body.Instructions;

                for (var index = 0; index < instructions.Count; index++)
                {
                    var instruction = instructions[index];

                    if (instruction.OpCode.Code is not Code.Call)
                        continue;

                    if (instruction.Operand is not IMethod calledMethod)
                        continue;

                    if (calledMethod.DeclaringType is null)
                        continue;

                    if (calledMethod.DeclaringType.Name != nameof(Extrinsics))
                        continue;

                    var methodName = calledMethod.Name;
                    switch (methodName)
                    {
                        case nameof(Extrinsics.Pinnable):
                            {
                                if (index < 1)
                                    throw new Exception("Extrinsics: [Pinnable] impossible call: no arguments");

                                var prevInstruction = instructions[index - 1];
                                if (!prevInstruction.IsLdlocOrLdloca())
                                    throw new Exception("Extrinsics: [Pinnable] passed variable is not a local");

                                var local = prevInstruction.GetLocal(body.Variables.Locals);

                                var oldTypeSig = local.Type;
                                if (!oldTypeSig.IsPinned)
                                {
                                    var newTypeSig = new PinnedSig(oldTypeSig);
                                    local.Type = newTypeSig;

                                    Console.WriteLine($"Set pinned state for local '{local}'");
                                }

                                RemoveInstruction(instructions, --index);
                                RemoveInstruction(instructions, index);
                                break;
                            }
                        case nameof(Extrinsics.Uninitialized):
                            {
                                RemoveInstruction(instructions, index--); // call
                                RemoveInstruction(instructions, index--); // ldloc
                                break;
                            }
                        case nameof(Extrinsics.GetTypeHandle):
                            {
                                if (calledMethod is not MethodSpec calledMethodSpec)
                                    throw new Exception("Extrinsics: [GetTypeHandle] the method is not generic");

                                var genericSignature = calledMethodSpec.GenericInstMethodSig;
                                var genericArgument = genericSignature.GenericArguments.First();
                                var typeSpec = new TypeSpecUser(genericArgument);

                                SetInstruction(instructions, index, OpCodes.Ldtoken.ToInstruction(typeSpec));
                                break;
                            }
                        case nameof(Extrinsics.LoadEffectiveAddress):
                            {
                                SetInstruction(instructions, index, new Instruction(OpCodes.Add)); // call -> add
                                break;
                            }
                        case nameof(Extrinsics.As):
                            {
                                RemoveInstruction(instructions, index--); // call

                                if (index >= 0 && instructions[index].OpCode.Code == Code.Box)
                                    RemoveInstruction(instructions, index--); // box primitive, because As's argument is object.

                                break;
                            }
                    }
                }
            }
        }
    }
}