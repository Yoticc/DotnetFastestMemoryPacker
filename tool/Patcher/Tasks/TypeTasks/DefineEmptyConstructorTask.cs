using dnlib.DotNet;
using dnlib.DotNet.Emit;

record DefineEmptyConstructorTask(TypeDef type) : TypeTask(type)
{
    public override void Execute()
    {
        var attributeType = this.GetCorlibTypeRef("System", "Attribute");

        var module = type.Module;
        var ctorSignature = MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String);
        var ctorAttributes = MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        var constructor = new MethodDefUser(".ctor", ctorSignature, MethodImplAttributes.IL, ctorAttributes);
        var body = constructor.Body = new CilBody();
        var instructions = body.Instructions;
        instructions.Add(new Instruction(OpCodes.Ldarg_0));
        instructions.Add(new Instruction(OpCodes.Call, attributeType));
        instructions.Add(new Instruction(OpCodes.Ret));


        Emitter.AddMethod(type, constructor);
    }

    public override string GetMessage() => $"Define empty constructor for type '{type.Name}'";
}