using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal sealed class ErlDriverThreadWait420 : WaitHandle
{
    internal ErlDriverThreadWait420(SafeWaitHandle handle) => SafeWaitHandle = handle;
}
