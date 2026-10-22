using System.Runtime.InteropServices;
namespace Erlang;

internal sealed unsafe partial class CountersArray421
{
    private readonly object allocationWindowGate434 = new();
    private bool allocationWindowLive434 = true;
    internal bool IsAllocated434
    {
        get
        {
            lock (allocationWindowGate434) return allocationWindowLive434;
        }
    }

    private void ReleaseNativeWindow434(void* pointer)
    {
        lock (allocationWindowGate434)
        {
            NativeMemory.Free(pointer);
            allocationWindowLive434 = false;
        }
    }
}
