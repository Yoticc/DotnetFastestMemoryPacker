using dnlib.DotNet;
using dnlib.DotNet.Emit;

partial class Program
{
    void DefineIgnoresAccessChecksPhase()
    {
        var importer = new Importer(module);
        var attributeType = GetCorlibTypeDef("System", "Attribute");
        var attributeCtorDef = attributeType.FindDefaultConstructor();
        var attributeCtorRef = importer.Import(attributeCtorDef);

        DefineType("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute", importer.Import(attributeType));
        var definedType = module.Find("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", false);

        var ctorSignature = MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String);
        var ctorAttributes = MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        var constructor = new MethodDefUser(".ctor", ctorSignature, MethodImplAttributes.IL, ctorAttributes);
        var body = constructor.Body = new CilBody();
        var instructions = body.Instructions;
        instructions.Add(new Instruction(OpCodes.Ldarg_0));
        instructions.Add(new Instruction(OpCodes.Call, attributeCtorRef));
        instructions.Add(new Instruction(OpCodes.Ret));
        AddMethod(definedType, constructor);

        var accessToLibrary = "System.Private.CoreLib";
        var moduleAttribute = new CustomAttribute(constructor, (CAArgument[])[new CAArgument(module.CorLibTypes.String, (UTF8String)accessToLibrary)]);
        module.Assembly.CustomAttributes.Add(moduleAttribute);
    }
}