using dnlib.DotNet;
using dnlib.DotNet.Emit;
using PatcherReference;

public class TransitInliner
{
    public static void Execute(ModuleDef module)
    {
        var transitMethodDefs = FindTransitMethodDefs(module);
        var methods = ConstructMethodsStructure(module, transitMethodDefs);
        InlineMethods(methods);
        RemoveMethodDefinitions(transitMethodDefs);
    }

    static List<MethodDef> FindTransitMethodDefs(ModuleDef module)
    {
        var methodDefs = new List<MethodDef>();
        foreach (var type in module.GetTypes())
        {
            foreach (var methodDef in type.Methods)
            {
                var hasTransitAttribute = methodDef.CustomAttributes.Any(attribute => attribute.AttributeType.Name == nameof(TransitMethodAttribute));
                if (!hasTransitAttribute)
                    continue;
                
                methodDefs.Add(methodDef);
            }
        }

        return methodDefs;
    }

    static List<Method> ConstructMethodsStructure(ModuleDef module, List<MethodDef> transitMethodDefs)
    {
        var transitMethods = new List<Method>();
        foreach (var transitMethodDef in transitMethodDefs)
        {
            var method = new Method(transitMethodDef);
            transitMethods.Add(method);
        }

        var methods = new List<Method>();
        foreach (var type in module.GetTypes())
        {
            foreach (var methodDef in type.Methods)
            {
                var body = methodDef.Body;
                var instructions = body.Instructions;

                foreach (var instruction in instructions)
                {
                    if (instruction.OpCode.Code is not Code.Call)
                        continue;

                    MethodDef calledMethod;
                    var operand = instruction.Operand;
                    if (operand is MethodSpec operandMethodSpec)
                    {
                        var methodDefOrRef = operandMethodSpec.Method;
                        if (!methodDefOrRef.IsMethodDef)
                            continue;

                        var calledMethodDef = operandMethodSpec.Method as MethodDef;
                        if (calledMethodDef is null)
                            continue;
                        calledMethod = calledMethodDef;
                    }
                    else if (operand is MethodDef operandMethodDef)
                    {
                        calledMethod = operandMethodDef;
                    }
                    else continue;

                    if (!transitMethodDefs.Contains(calledMethod))
                        continue;

                    var calledTransitMethod = transitMethods.Find(method => method.MethodDefinition == calledMethod);
                    if (calledTransitMethod is null)
                        throw new Exception($"TransitInliner.ConstructMethodsStructure: Cannot find constructed transit method.");

                    var method = methods.Find(method => method.MethodDefinition == methodDef);
                    if (method is null)
                    {
                        method = transitMethods.Find(method => method.MethodDefinition == methodDef);
                        if (method is null)
                        {
                            method = new Method(methodDef);
                            methods.Add(method);
                        }
                    }

                    method.CalledTransitMethods.Add(calledTransitMethod);
                }
            }
        }

        foreach (var transitMethod in transitMethods)
        {
            if (transitMethod.CalledTransitMethods.Count == 0)
                continue;

            methods.Add(transitMethod);
        }

        return methods;
    }

    static void InlineMethods(List<Method> methods)
    {
        while (methods.Count > 0)
        {
            var handledMethods = new List<Method>();

            foreach (var method in methods)
            {
                if (method.CalledTransitMethods.Count == 0)
                    throw new Exception(
                        "TransitInliner.InlineMethods: Some problem related method structure construction.\n" +
                        "A method without transit calls has been added to the list of methods."
                    );

                foreach (var transitMethod in method.CalledTransitMethods)
                {
                    if (transitMethod.CalledTransitMethods.Count != 0)
                        goto Next;
                }

                InlineMethod(method);

                handledMethods.Add(method);

            Next: { }
            }

            foreach (var handledMethod in handledMethods)
            {
                methods.Remove(handledMethod);
                handledMethod.CalledTransitMethods.Clear();
            }
        }
    }

    static void InlineMethod(Method method)
    {
        var calledTransitMethods = method.CalledTransitMethods;
        var methodDef = method.MethodDefinition;

        var body = methodDef.Body;
        var arguments = methodDef.GetArguments();
        var localsList = body.Variables.Locals;

        var instructions = body.Instructions;
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];

            if (instruction.OpCode.Code is not Code.Call)
                continue;

            MethodDef calledMethod;
            List<TypeSig>? genericArguments;

            var operand = instruction.Operand;
            if (operand is MethodSpec operandMethodSpec)
            {
                var methodDefOrRef = operandMethodSpec.Method;
                if (!methodDefOrRef.IsMethodDef)
                    continue;

                var calledMethodDef = operandMethodSpec.Method as MethodDef;
                if (calledMethodDef is null)
                    continue;
                calledMethod = calledMethodDef;

                var instantiation = operandMethodSpec.Instantiation as GenericInstMethodSig;
                if (instantiation is null)
                    continue;

                genericArguments = instantiation.GenericArguments.ToList();
            }
            else if (operand is MethodDef operandMethodDef)
            {
                calledMethod = operandMethodDef;
                genericArguments = null;
            }
            else continue;

            var calledTransitMethod = calledTransitMethods.Find(method => method.MethodDefinition == calledMethod);
            if (calledTransitMethod is null)
                continue;

            var calledTransitMethodDef = calledTransitMethod.MethodDefinition;
            var tBody = calledTransitMethodDef.Body;

            var tGenericParameters = calledTransitMethodDef.GenericParameters;
            var tLocals = tBody.Variables.Locals;
            var tArguments = calledTransitMethodDef.GetArguments();

            var localPairsStartIndex = localsList.Count;
            for (var tLocalIndex = 0; tLocalIndex < tLocals.Count; tLocalIndex++)
            {
                var tLocal = tLocals[tLocalIndex];
                var local = new Local(tLocal.Type, null, localPairsStartIndex + tLocal.Index);
                localsList.Add(local);
            }
            var locals = localsList.ToArray();

            Variable? returnParameterVariable = null;
            if (calledTransitMethodDef.GetReturnParameter() is not null)
            {
                if (instructionIndex + 1 != instructions.Count)
                {
                    var storeValueFromMethodToVariableInstruction = instructions[instructionIndex + 1];
                    returnParameterVariable = Variable.ExtractFromLoadingInstruction(storeValueFromMethodToVariableInstruction);
                }
            }

            var instructionCursor = instructionIndex - 1;
            var variables = new (Variable Variable, Variable TransitVariable)[tArguments.Length];
            for (var variableIndex = tArguments.Length - 1; variableIndex >= 0; )
            {
                if (instructionCursor == -1)
                    throw new Exception();

                var pInstruction = instructions[instructionCursor--];
                var pOpCode = pInstruction.OpCode.Code;

                if (pOpCode == Code.Dup)
                {
                    var pPreviousInstruction = instructions[instructionCursor];
                    if (!Variable.IsInstructionContainsStoringVariable(pPreviousInstruction))
                        throw new Exception("TransitInliner.InlineMethod: The arguments detecting algorithm has failed.");

                    var variable = Variable.ExtractFromStoringInstruction(pPreviousInstruction);
                    var tVariable = VariableArgument.FromParameter(tArguments[variableIndex]);

                    variables[variableIndex--] = (variable, tVariable);
                }
                else if (Variable.IsInstructionContainsLoadingVariable(pInstruction))
                {
                    var variable = Variable.ExtractFromLoadingInstruction(pInstruction);
                    var tVariable = VariableArgument.FromParameter(tArguments[variableIndex]);

                    variables[variableIndex--] = (variable, tVariable);
                }
            }
            instructionCursor++;

            // remove prologue
            int calledMethodPrologueSize = instructionIndex - instructionCursor;
            for (var i = 0; i < calledMethodPrologueSize; i++)
                instructions.RemoveAt(instructionCursor);
            instructionIndex -= calledMethodPrologueSize;

            // remove call
            instructions.RemoveAt(instructionCursor);
            instructionIndex--;

            var tInstuctions = tBody.Instructions;
            BranchifyInsturctions(tInstuctions, out var relocations);

            for (var tInstructionIndex = 0; tInstructionIndex < tInstuctions.Count; tInstructionIndex++)
            {
                var tInstuction = tInstuctions[tInstructionIndex];
                var tOpCode = tInstuction.OpCode.Code;

                if (tOpCode.IsArgumentRelated())
                {
                    var variableIndex = -1;
                    for (var argumentIndex = 0; argumentIndex < variables.Length; argumentIndex++)
                    {
                        var transitArgument = variables[argumentIndex].TransitVariable;
                        if (transitArgument.IsInstructionRelatedToVariable(tInstuction))
                        {
                            variableIndex = argumentIndex;
                            break;
                        }
                    }

                    var argument = variables[variableIndex];
                    if (tOpCode.IsLdargOrLdarga())
                        tInstuction = argument.Variable.GetLoadInstruction(arguments, locals);
                    else if (tOpCode.IsStarg())
                        tInstuction = argument.Variable.GetStoreInstruction(arguments, locals);

                }
                else if (tOpCode.IsLocalRelated())
                {
                    if (tOpCode.IsLdloc())
                        tInstuction = VariableLocal.GetLoadInstruction(isReference: false, index: localPairsStartIndex + tInstuction.GetLdlocOperand(), locals);
                    else if (tOpCode.IsLdloca())
                        tInstuction = VariableLocal.GetLoadInstruction(isReference: true, index: localPairsStartIndex + tInstuction.GetLdlocaOperand(), locals);
                    else if (tOpCode.IsStloc())
                        tInstuction = VariableLocal.GetStoreInstruction(localPairsStartIndex + tInstuction.GetStlocOperand(), locals);
                }
                else if (tGenericParameters.Count > 0)
                {
                    var tOperand = tInstuction.Operand;
                    if (tOperand is TypeSpec typeSpec)
                    {
                        if (typeSpec.TypeSig is GenericMVar mvar)
                        {
                            var mvarIndex = mvar.Number;
                            if (genericArguments is null)
                                throw new Exception("TransitInliner.InlineMethod: Method to inline has generic arguments, but they not initialized.");

                            typeSpec.TypeSig = genericArguments[(int)mvarIndex];
                        }
                    }
                }

                instructions.Insert(++instructionIndex, tInstuction);
            }

            foreach (var relocation in relocations)
            {
                instruction = instructions[instructionCursor + relocation.InstructionOffset];
                var targetInstruction = instructions[instructionCursor + relocation.TargetInstructionOffset];
                instruction.Operand = targetInstruction;
            }
        }

        instructions.UpdateInstructionOffsets();
    }

    // converts return statements to jumps, and store information about original relocations
    static void BranchifyInsturctions(IList<Instruction> instructions, out List<Relocation> relocations)
    {
        relocations = new List<Relocation>();

        var hasMultipleExits = false;
        var returnInstructionIndices = new List<int>();
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            if (instructions[instructionIndex].OpCode.Code == Code.Ret)
            {
                if (instructionIndex == instructions.Count - 1)
                {
                    instructions.RemoveAt(instructionIndex);
                    break;
                }

                hasMultipleExits = true;
                returnInstructionIndices.Add(instructionIndex);
            }
        }

        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (instruction.IsConditionalBranch())
            {
                var operandInstruction = instruction.Operand as Instruction;
                if (operandInstruction is null)
                    throw new NullReferenceException("TransitInliner.GetBranchedInsturctions: Unknown instruction in branch instruction.");

                var operandInstructionIndex = instructions.IndexOf(operandInstruction);
                if (operandInstructionIndex == -1)
                    throw new Exception("TransitInliner.GetBranchedInsturctions: Branch instruction's operand target to an out of bound instruction.");

                var relocation = new Relocation(instructionIndex, operandInstructionIndex);
                relocations.Add(relocation);
            }
        }

        if (hasMultipleExits)
        {
            var exitTargetInsturctionIndex = instructions.Count;

            foreach (var returnInstructionIndex in returnInstructionIndices)
            {
                var jumpInstruction = new Instruction(OpCodes.Br, null/*next instruction after inlined method*/);
                instructions[returnInstructionIndex] = jumpInstruction;

                var relocation = new Relocation(returnInstructionIndex, exitTargetInsturctionIndex);
                relocations.Add(relocation);
            }
        }
    }

    static void RemoveMethodDefinitions(List<MethodDef> methods)
    {
        foreach (var method in methods)
            method.DeclaringType.Remove(method);
    }

    record Relocation(int InstructionOffset, int TargetInstructionOffset);

    class Method
    {
        public Method(MethodDef methodDef)
        {
            MethodDefinition = methodDef;
            CalledTransitMethods = [];
        }

        public MethodDef MethodDefinition;
        public List<Method> CalledTransitMethods;
    }

    abstract class Variable
    {
        public int Index;
        public bool IsReference;
        
        public abstract bool IsInstructionRelatedToVariable(Instruction instruction);

        public Instruction GetLoadInstruction(Parameter[] parameters, Local[] locals)
        {
            if (this is VariableLocal local)
                return local.GetLoadInstruction(locals);
            else if (this is VariableArgument argument)
                return argument.GetLoadInstruction(parameters);

            throw new Exception();
        }

        public Instruction GetStoreInstruction(Parameter[] parameters, Local[] locals)
        {
            if (this is VariableLocal local)
                return local.GetStoreInstruction(locals);
            else if (this is VariableArgument argument)
                return argument.GetStoreInstruction(parameters);

            throw new Exception();
        }

        public static bool IsInstructionContainsVariable(Instruction instruction) => IsOpCodeContainsVariable(instruction.OpCode.Code);

        public static bool IsInstructionContainsLoadingVariable(Instruction instruction) => IsOpCodeContainsLoadingVariable(instruction.OpCode.Code);

        public static bool IsInstructionContainsStoringVariable(Instruction instruction) => IsOpCodeContainsStoringVariable(instruction.OpCode.Code);

        public static bool IsOpCodeContainsVariable(Code code) => VariableLocal.IsVariable(code) || VariableArgument.IsVariable(code);

        public static bool IsOpCodeContainsLoadingVariable(Code code) => VariableLocal.IsLoadingVariable(code) || VariableArgument.IsLoadingVariable(code);

        public static bool IsOpCodeContainsStoringVariable(Code code) => VariableLocal.IsStoringVariable(code) || VariableArgument.IsStoringVariable(code);

        public static Variable ExtractFromLoadingInstruction(Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            if (VariableLocal.IsVariable(code))
                return VariableLocal.ExtractFromLoadingInstruction(instruction);

            if (VariableArgument.IsVariable(code))
                return VariableArgument.ExtractFromLoadingInstruction(instruction);

            throw new Exception("Variable.ExtractFromLoadingInstruction: The instruction is not suitable for variable");
        }

        public static Variable ExtractFromStoringInstruction(Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            if (VariableLocal.IsVariable(code))
                return VariableLocal.ExtractFromStoringInstruction(instruction);

            if (VariableArgument.IsVariable(code))
                return VariableArgument.ExtractFromStoringInstruction(instruction);

            throw new Exception("Variable.ExtractFromStoringInstruction: The instruction is not suitable for variable");
        }
    }

    class VariableLocal : Variable
    {
        public override bool IsInstructionRelatedToVariable(Instruction instruction)
        {
            var opcode = instruction.OpCode.Code;
            var index = -1;
            if (opcode.IsLdloc())
                index = instruction.GetLdlocOperand();
            else if (opcode.IsLdloca())
                index = instruction.GetLdlocaOperand();
            else if (opcode.IsStloc())
                index = instruction.GetStlocOperand();

            return Index == index;
        }

        public Instruction GetLoadInstruction(Local[] locals) => GetLoadInstruction(IsReference, Index, locals);

        public Instruction GetStoreInstruction(Local[] locals) => GetStoreInstruction(Index, locals);

        public static Instruction GetLoadInstruction(bool isReference, int index, Local[] locals)
        {
            return isReference switch
            {
                true => index switch
                {
                    <= byte.MaxValue => OpCodes.Ldloca_S.ToInstruction(locals[index]),
                    _ => OpCodes.Ldloca.ToInstruction(locals[index]),
                },

                false => index switch
                {
                    0 => OpCodes.Ldloc_0.ToInstruction(),
                    1 => OpCodes.Ldloc_1.ToInstruction(),
                    2 => OpCodes.Ldloc_2.ToInstruction(),
                    3 => OpCodes.Ldloc_3.ToInstruction(),
                    <= byte.MaxValue => OpCodes.Ldloc_S.ToInstruction(locals[index]),
                    _ => OpCodes.Ldloc.ToInstruction(locals[index]),
                },
            };
        }

        public static Instruction GetStoreInstruction(int index, Local[] locals)
        {
            return index switch
            {
                0 => OpCodes.Stloc_0.ToInstruction(),
                1 => OpCodes.Stloc_1.ToInstruction(),
                2 => OpCodes.Stloc_2.ToInstruction(),
                3 => OpCodes.Stloc_3.ToInstruction(),
                <= byte.MaxValue => OpCodes.Stloc_S.ToInstruction(locals[index]),
                _ => OpCodes.Stloc.ToInstruction(locals[index]),
            };
        }

        public static bool IsVariable(Code code) => IsLoadingVariable(code) || IsStoringVariable(code);

        public static bool IsLoadingVariable(Code code) => code.IsLdlocOrLdloca();

        public static bool IsStoringVariable(Code code) => code.IsStloc();

        public static new VariableLocal ExtractFromLoadingInstruction(Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            if (code.IsLdloc())
            {
                return new VariableLocal
                {
                    Index = instruction.GetLdlocOperand(),
                    IsReference = false
                };
            }
            else if (code.IsLdloca())
            {
                return new VariableLocal
                {
                    Index = instruction.GetLdlocaOperand(),
                    IsReference = false
                };
            }
            else throw new Exception();
        }

        public static new VariableLocal ExtractFromStoringInstruction(Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            if (code.IsStloc())
            {
                return new VariableLocal
                {
                    Index = instruction.GetStlocOperand(),
                    IsReference = false
                };
            }
            else throw new Exception();
        }
    }

    class VariableArgument : Variable
    {
        public override bool IsInstructionRelatedToVariable(Instruction instruction)
        {
            var opcode = instruction.OpCode.Code;
            var index = -1;
            if (opcode.IsLdarg())
                index = instruction.GetLdargOperand();
            else if (opcode.IsLdarga())
                index = instruction.GetLdargaOperand();
            else if (opcode.IsStarg())
                index = instruction.GetStargOperand();

            return Index == index;
        }

        public Instruction GetLoadInstruction(Parameter[] parameters)
        {
            return IsReference switch
            {
                true => Index switch
                {
                    <= byte.MaxValue => OpCodes.Ldloca_S.ToInstruction(parameters[Index]),
                    _ => OpCodes.Ldloca.ToInstruction(parameters[Index]),
                },

                false => Index switch
                {
                    0 => OpCodes.Ldarg_0.ToInstruction(),
                    1 => OpCodes.Ldarg_1.ToInstruction(),
                    2 => OpCodes.Ldarg_2.ToInstruction(),
                    3 => OpCodes.Ldarg_3.ToInstruction(),
                    <= byte.MaxValue => OpCodes.Ldarg_S.ToInstruction(parameters[Index]),
                    _ => OpCodes.Ldarg.ToInstruction(parameters[Index]),
                },
            };
        }

        public Instruction GetStoreInstruction(Parameter[] parameters)
        {
            return Index switch
            {
                <= byte.MaxValue => OpCodes.Starg_S.ToInstruction(parameters[Index]),
                _ => OpCodes.Starg.ToInstruction(parameters[Index]),
            };
        }

        public static bool IsVariable(Code code) => IsLoadingVariable(code) || IsStoringVariable(code);

        public static bool IsLoadingVariable(Code code) => code.IsLdargOrLdarga();

        public static bool IsStoringVariable(Code code) => code.IsStarg();

        public static new VariableArgument ExtractFromLoadingInstruction(Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            if (code.IsLdarg())
            {
                return new VariableArgument
                {
                    Index = instruction.GetLdargOperand(),
                    IsReference = false
                };
            }
            else if (code.IsLdarga())
            {
                return new VariableArgument
                {
                    Index = instruction.GetLdargaOperand(),
                    IsReference = false
                };
            }
            else throw new Exception();
        }

        public static new VariableArgument ExtractFromStoringInstruction(Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            if (code.IsStarg())
            {
                return new VariableArgument
                {
                    Index = instruction.GetStargOperand(),
                    IsReference = false
                };
            }
            else throw new Exception();
        }

        public static VariableArgument FromParameter(Parameter parameter)
        {
            return new VariableArgument
            {
                Index = parameter.Index,
                IsReference = false
            };
        }
    }
}