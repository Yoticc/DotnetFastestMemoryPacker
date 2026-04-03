using dnlib.DotNet;
using dnlib.DotNet.Emit;

partial class Program
{
    void ImplementUnsafeAccessorsPhase()
    {
        foreach (var type in module.Types)
        {
            var methods = type.Methods;
            for (var methodIndex = 0; methodIndex < methods.Count; methodIndex++)
            {
                var method = methods[methodIndex];
                if (!method.HasBody)
                    continue;

                var body = method.Body;
                var instructions = body.Instructions;
                for (var index = 0; index < instructions.Count; index++)
                {
                    var instruction = instructions[index];

                    if (instruction.OpCode.Code is not Code.Call)
                        continue;

                    if (instruction.Operand is not MethodDef calledMethod)
                        continue;

                    if (!GetUnsafeAccessAttributeArguments(calledMethod, out var typeName, out var methodName, out var methodSignature))
                        continue;

                    var declaringType = corlibModule.Find(typeName, false);

                    MethodDef? attributedMethod;
                    if (methodSignature is not null)
                        attributedMethod = declaringType.Methods.FirstOrDefault(m => m.Name == methodName && m.ToString() == methodSignature);
                    else attributedMethod = declaringType.Methods.FirstOrDefault(m => m.Name == methodName);

                    if (attributedMethod is null)
                        throw new Exception($"HandleUnsafeAccessors: can not find a method with name '{methodName}' in type '{typeName}'");

                    if (calledMethod.Parameters.Count != attributedMethod.Parameters.Count)
                    {
                        var availableSignatures = string.Join('\n', declaringType.Methods.Where(m => m.Name == methodName).Select(m => $"'{m}'"));
                        throw new Exception(
                            $"HandleUnsafeAccessors: miscounting of arguments for method '{methodName} in type '{typeName}'.\n" +
                            $"Available signatures: {availableSignatures}"
                        );
                    }

                    var importer = new Importer(module);
                    var importedMethod = importer.Import(attributedMethod);
                    if (importedMethod is null)
                        throw new Exception($"HandleUnsafeAccessors: can not import found method with name '{methodName}' in type '{typeName}'");

                    instruction.Operand = importedMethod;
                    Console.WriteLine($"Rewrite implementation for method '{method.Name}'");
                }
            }
        }

        foreach (var type in module.Types)
        {
            var methods = type.Methods;
            for (var methodIndex = 0; methodIndex < methods.Count;)
            {
                var method = methods[methodIndex];
                if (HasUnsafeAccessAttribute(method))
                    RemoveMethod(type, methodIndex);
                else methodIndex++;
            }
        }
    }
}