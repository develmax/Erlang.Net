using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ReBuffer411 : IDisposable
{
    internal ReBuffer411(byte[] bytes)
    {
        var zero = new byte[Math.Max(bytes.Length, IntPtr.Size)];
        bytes.CopyTo(zero, 0);
        Pointer = Marshal.AllocHGlobal(zero.Length);
        try
        {
            Marshal.Copy(
                zero,
                0,
                Pointer,
                zero.Length
            );
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal IntPtr Pointer { get; private set; }

    public void Dispose()
    {
        var pointer = Pointer;
        Pointer = IntPtr.Zero;
        if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer);
    }
}
