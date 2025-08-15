using dnlib.DotNet;

class Patcher
{
    public void Execute(ModuleDefMD module)
    {
        var type = module.Find("DotnetFastestMemoryPacker.FastestMemoryPacker", false);

        SetLocalPinned(type, "IndexOf");
        SetLocalPinned(type, "Pack");

        static void SetLocalPinned(TypeDef type, string methodName, params string[] localNames)
        {
            var method = type.Methods.First(method => method.Name == methodName);
            var locals = method.Body.Variables.Locals;

            foreach (var localName in localNames)
            {
                var local = locals.FirstOrDefault(local => local.Name == localName);
                if (local is not null)
                {
                    var oldTypeSig = local.Type;
                    if (!oldTypeSig.IsPinned)
                    {
                        var newTypeSig = new PinnedSig(oldTypeSig);
                        local.Type = newTypeSig;
                        Console.WriteLine($"Set pinned state for local {localName} for method {methodName}");
                    }
                }
            }
        }
    }
}