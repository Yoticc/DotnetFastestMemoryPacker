namespace PatcherReference;

// also known as TransitMethod, this name is just more comfortable
[AttributeUsage(AttributeTargets.Method)]
public class InlineAttribute : Attribute;