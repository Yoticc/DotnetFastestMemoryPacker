using dnlib.DotNet;

class Patcher
{
    public void Execute(ModuleDefMD module)
    {
        var type = module.Find("DotnetFastestMemoryPacker.FastestMemoryPacker", false);
        var method = type.Methods.First(method => method.FullName == "System.Byte[] DotnetFastestMemoryPacker.FastestMemoryPacker::Pack(System.Object)");
        var body = method.Body;

        var local = body.Variables.Locals.FirstOrDefault(l => l.Name == "object");
        if (local is not null)
        {
            var oldTypeSig = local.Type;
            if (!oldTypeSig.IsPinned)
            {
                var newTypeSig = new PinnedSig(oldTypeSig);
                local.Type = newTypeSig;
            }
        }
    }
}