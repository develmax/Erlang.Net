using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class NativePortStringVector : IDisposable
{
    private readonly List<nint> strings = new();
    public nint Pointer { get; private set; }

    public NativePortStringVector(IEnumerable<string> values)
    {
        try
        {
            foreach (string value in values)
                strings.Add(Marshal.StringToCoTaskMemUTF8(value));
            Pointer = Marshal.AllocHGlobal(checked((strings.Count + 1) * IntPtr.Size));
            for (int index = 0; index < strings.Count; index++)
                Marshal.WriteIntPtr(Pointer, index * IntPtr.Size, strings[index]);
            Marshal.WriteIntPtr(Pointer, strings.Count * IntPtr.Size, 0);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        foreach (nint text in strings)
            Marshal.FreeCoTaskMem(text);
        strings.Clear();
        if (Pointer != 0)
            Marshal.FreeHGlobal(Pointer);
        Pointer = 0;
    }
}
