using System.Runtime.InteropServices;

static class PInvoke
{
    [DllImport("ntdll.dll")]
    public static extern void NtSuspendProcess(nint processHandle);
}
