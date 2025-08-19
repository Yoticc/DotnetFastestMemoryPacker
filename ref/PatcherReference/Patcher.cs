namespace PatcherReference;
public class Patcher
{
    public static void Pinnable<T>(out T value) => value = default;
}