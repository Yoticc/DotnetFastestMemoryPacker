namespace PatcherReference;

// i got tired of writing [ M e t h o d I m p l ( M e t h o d I m p l O p t i o n s . A g g r e s s i v e I n l i n i n g ) ] for every member,
// so i implemented a separate attribute to forces all methods to be inlined by jit.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class InlineAllMembersAttribute : Attribute;