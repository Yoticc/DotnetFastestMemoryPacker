namespace PatcherReference;

[AttributeUsage(AttributeTargets.Method)]
public class UnsafeAccessAttribute(string DeclaringTypeFullName, string? MethodName = null, string? MethodSignature = null) : Attribute;