using dnlib.DotNet;
using dnlib.DotNet.Emit;

record DefineIgnoresAccessChecksPhase() : Phase("Define IgnoresAccessChecksToMethod")
{
    public override void Execute()
    {
        var importer = new Importer(Module);
        var attributeType = GetCorlibTypeDef("System", "Attribute");
        var attributeCtorDef = attributeType.FindDefaultConstructor();
        var attributeCtorRef = importer.Import(attributeCtorDef);

        DefineType("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute", importer.Import(attributeType));
        var definedType = Module.Find("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", false);

        var ctorSignature = MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.String);
        var ctorAttributes = MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        var constructor = new MethodDefUser(".ctor", ctorSignature, MethodImplAttributes.IL, ctorAttributes);
        var body = constructor.Body = new CilBody();
        var instructions = body.Instructions;
        instructions.Add(new Instruction(OpCodes.Ldarg_0));
        instructions.Add(new Instruction(OpCodes.Call, attributeCtorRef));
        instructions.Add(new Instruction(OpCodes.Ret));
        Emitter.AddMethod(definedType, constructor);

        var accessToLibrary = "System.Private.CoreLib";
        var moduleAttribute = new CustomAttribute(constructor, (CAArgument[])[new CAArgument(Module.CorLibTypes.String, (UTF8String)accessToLibrary)]);
        Module.Assembly.CustomAttributes.Add(moduleAttribute);
    }
}