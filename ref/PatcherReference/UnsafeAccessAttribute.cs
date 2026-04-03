namespace PatcherReference;

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Method)]
public class UnsafeAccessAttribute(string DeclaringTypeFullName, string? MethodName = null, string? MethodSignature = null) : Attribute;