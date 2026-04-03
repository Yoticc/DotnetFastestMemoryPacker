using dnlib.DotNet;
using dnlib.DotNet.Emit;
using PatcherReference;

partial class Program
{
    public void InlineTransitMethodsPhase()
    {
        var transitMethodDefs = GetTransitMethods(module);
        SimplifyMethods(transitMethodDefs);

        var methods = ConstructMethodsStructure(module, transitMethodDefs, out var methodDefs);

        SimplifyMethods(methodDefs);
        InlineMethods(methods);
        OptimizeMethods(methodDefs);

        RemoveTransitMethodDefinitions(transitMethodDefs);

        CustomAttribute? GetTransitMethodAttribute(MethodDef methodOwner) => GetAttibute(methodOwner, nameof(InlineAttribute));

        bool HasTransitMethodAttribute(MethodDef methodOwner) => GetTransitMethodAttribute(methodOwner) is not null;

        List<MethodDef> GetTransitMethods(ModuleDef module) => module.GetTypes().SelectMany(type => type.Methods).Where(HasTransitMethodAttribute).ToList();

        void SimplifyMethods(List<MethodDef> methods)
        {
            foreach (var method in methods)
                method.SimplifyInstructions();
        }

        void OptimizeMethods(List<MethodDef> methods)
        {
            foreach (var method in methods)
                method.OptimizeInstructions();
        }

        List<TransitMethod> ConstructMethodsStructure(ModuleDef module, List<MethodDef> transitMethodDefs, out List<MethodDef> methodDefs)
        {
            methodDefs = new List<MethodDef>();

            var transitMethods = new List<TransitMethod>();
            foreach (var transitMethodDef in transitMethodDefs)
            {
                var method = new TransitMethod(transitMethodDef);
                transitMethods.Add(method);
            }

            var methods = new List<TransitMethod>();
            foreach (var type in module.GetTypes())
            {
                foreach (var methodDef in type.Methods)
                {
                    if (!methodDef.HasBody)
                        continue;

                    var body = methodDef.Body;
                    var instructions = body.Instructions;
                    foreach (var instruction in instructions)
                    {
                        if (instruction.OpCode.Code is not Code.Call)
                            continue;

                        var calledMethod = instruction.TryExtractMethodDefinition();
                        if (calledMethod is null)
                            continue;

                        if (!transitMethodDefs.Contains(calledMethod))
                            continue;

                        var calledTransitMethod = transitMethods.Find(method => method.MethodDefinition == calledMethod);
                        if (calledTransitMethod is null)
                            throw new Exception($"TransitInliner.ConstructMethodsStructure: Cannot find constructed transit method.");

                        var method = methods.Find(method => method.MethodDefinition == methodDef);
                        if (method is null)
                        {
                            methodDefs.Add(methodDef);

                            method = transitMethods.Find(method => method.MethodDefinition == methodDef);
                            if (method is null)
                            {
                                method = new TransitMethod(methodDef);
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

        void InlineMethods(List<TransitMethod> methods)
        {
            while (methods.Count > 0)
            {
                var handledMethods = new List<TransitMethod>();

                foreach (var method in methods)
                {
                    if (method.CalledTransitMethods.Count == 0)
                        throw new Exception(
                            "TransitInliner.InlineMethods: Some problem related method structure construction.\n" +
                            "A method without transit calls has been added to the list of methods."
                        );

                    if (method.CalledTransitMethods.Any(transitMethod => transitMethod.CalledTransitMethods.Count != 0))
                        continue;

                    var methodDef = method.MethodDefinition;
                    InlineMethod(method);

                    handledMethods.Add(method);
                }

                foreach (var handledMethod in handledMethods)
                {
                    methods.Remove(handledMethod);
                    handledMethod.CalledTransitMethods.Clear();
                }
            }
        }

        void InlineMethod(TransitMethod method)
        {
            var transitMethods = method.CalledTransitMethods;
            var methodDef = method.MethodDefinition;
            var body = methodDef.Body;
            var arguments = methodDef.GetArguments().ToArray();
            var locals = body.Variables.Locals;

            var instructions = body.Instructions;
            for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                var instruction = instructions[instructionIndex];

                if (instruction.OpCode.Code is not Code.Call)
                    continue;

                instruction.TryExtractMethodDefinitionAndInstantiation(out var calledMethod, out var instantiation);
                var instantiationGenericArguments = instantiation is not null ? instantiation.GenericArguments.ToList() : [];

                var tMethod = transitMethods.Find(method => method.MethodDefinition == calledMethod);
                if (tMethod is null)
                    continue;

                var transitMethodDef = tMethod.MethodDefinition;
                var transitBody = transitMethodDef.Body;
                var transitInstuctions = transitBody.Instructions.Clone();

                var transitGenericParameters = transitMethodDef.GenericParameters;
                for (var genericArgumentIndex = 0; genericArgumentIndex < transitGenericParameters.Count; genericArgumentIndex++)
                {
                    var genericArgument = transitGenericParameters[genericArgumentIndex];
                    var instantiationGenericArgument = instantiationGenericArguments[genericArgumentIndex];
                    ReplaceGenericWithArgument(transitInstuctions, genericArgument, instantiationGenericArgument);
                }

                var transitLocals = transitBody.Variables.Locals;
                for (var transitLocalIndex = 0; transitLocalIndex < transitLocals.Count; transitLocalIndex++)
                {
                    var transitLocal = transitLocals[transitLocalIndex];
                    var local = new Local(transitLocal.Type, null, locals.Count);
                    locals.Add(local);
                    ReplaceLocal(transitInstuctions, transitLocal, local);
                }

                var transitArguments = transitMethodDef.GetArguments();
                var mustOptimizeBranches = false;
                for (var argumentIndex = transitArguments.Count - 1; argumentIndex >= 0;)
                {
                    var argument = transitArguments[argumentIndex];
                    instruction = instructions[instructionIndex - 1];
                    var opcode = instruction.OpCode.Code;

                    if (opcode is Code.Ldc_I4 or Code.Ldarg or Code.Ldloc)
                    {
                        if (opcode is Code.Ldc_I4)
                        {
                            ReplaceArgumentWithConstant(transitInstuctions, argument, instruction);
                            mustOptimizeBranches = true;
                        }
                        else if (opcode is Code.Ldarg)
                        {
                            ReplaceArgument(transitInstuctions, argument, instruction.GetArgumentOperand());
                        }
                        else if (opcode is Code.Ldloc)
                        {
                            ReplaceArgument(transitInstuctions, argument, instruction.GetLocalOperand());
                        }

                        RemoveInstruction(instructions, --instructionIndex);
                        argumentIndex--;
                    }
                    else if (opcode.IsConv()) // in some cases it will break everything 😊
                    {
                        RemoveInstruction(instructions, --instructionIndex);
                    }
                    else
                    {
                        for (; argumentIndex >= 0; argumentIndex--)
                        {
                            argument = transitArguments[argumentIndex];
                            var local = new Local(argument.Type, null, locals.Count);
                            locals.Add(local);

                            var stlocInstruction = new Instruction(OpCodes.Stloc, local);
                            InsertInstruction(instructions, instructionIndex++, stlocInstruction);

                            ReplaceArgument(transitInstuctions, argument, local);
                        }
                    }
                }

                if (mustOptimizeBranches)
                    OptimizeConditionsAndBranches(transitInstuctions);

                var callOriginalMethodInstructionIndex = instructionIndex;
                var nextAfterCallInstructionIndex = instructionIndex + 1;
                BranchifyInsturctions(transitInstuctions, instructions[nextAfterCallInstructionIndex]);

                InsertInstructions(instructions, nextAfterCallInstructionIndex, transitInstuctions);
                instructionIndex += transitInstuctions.Count;

                RemoveInstruction(instructions, callOriginalMethodInstructionIndex);
                instructionIndex--;
            }

            instructions.UpdateInstructionOffsets();

            ReportOutsideDependencies(instructions);
        }

        void ReportOutsideDependencies(IList<Instruction> instructions)
        {
            var outsideDependencies = instructions.GetInstructionOutsideDependencies();
            if (outsideDependencies.Count != 0)
            {
                var dependenciesCount = outsideDependencies.Count;
                var outsideOperandOwners = instructions.GetInstructionsWithThoseOperandInstructions(outsideDependencies);

                Console.WriteLine("Found unresolved depedencies:");
                for (var index = 0; index < dependenciesCount; index++)
                {
                    var source = outsideOperandOwners[index];
                    var destination = outsideDependencies[index];
                    Console.WriteLine($"[{index}] IL_{source.Offset:X4}: {source.OpCode.Code} -> IL_{destination.Offset:X4}: {destination.OpCode.Code} {destination.Operand}");
                }
                Console.ReadLine();
            }
        }

        void BranchifyInsturctions(IList<Instruction> instructions, Instruction nextInstructionAfterInlinedCode)
        {
            if (nextInstructionAfterInlinedCode.OpCode.Code == Code.Ret)
                return;

            var hasMultipleReturnStatements = instructions.HasMultipleReturnStatements();

            for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                if (instructions[instructionIndex].OpCode.Code == Code.Ret)
                {
                    if (instructionIndex + 1 == instructions.Count && !hasMultipleReturnStatements)
                    {
                        RemoveInstructionWithDependencyReplacement(instructions, instructionIndex, nextInstructionAfterInlinedCode);
                        break;
                    }

                    instructions[instructionIndex] = new Instruction(OpCodes.Br, nextInstructionAfterInlinedCode);
                }
            }
        }

        void RemoveTransitMethodDefinitions(List<MethodDef> methods)
        {
            foreach (var method in methods)
                RemoveMethod(method);
        }
    }

    class TransitMethod
    {
        public TransitMethod(MethodDef methodDef)
        {
            MethodDefinition = methodDef;
            CalledTransitMethods = [];
        }

        public MethodDef MethodDefinition;
        public List<TransitMethod> CalledTransitMethods;
    }
}