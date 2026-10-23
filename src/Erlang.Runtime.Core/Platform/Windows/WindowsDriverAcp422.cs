using System.Runtime.InteropServices;
namespace Erlang;

internal static unsafe class WindowsDriverAcp422
{
    [DllImport(ErlDriverPrimitiveNames422.Kernel, EntryPoint = "MultiByteToWideChar", SetLastError = true)]
    private static extern int DecodeNative(
        uint page,
        uint flags,
        byte* input,
        int length,
        char* output,
        int capacity
    );

    internal static string? Decode(byte[] bytes)
    {
        int nul = Array.IndexOf(bytes, (byte)0);
        byte[] input = [.. (nul < 0 ? bytes.AsSpan() : bytes.AsSpan(0, nul)), 0];
        fixed (byte* pointer = input)
        {
            int length = DecodeNative(
                ErlDriverPrimitiveNames422.Acp,
                0,
                pointer,
                -1,
                null,
                0
            );
            if (length == 0) return null;
            char[] output = new char[length];
            fixed (char* destination = output)
                if (DecodeNative(
                    ErlDriverPrimitiveNames422.Acp,
                    0,
                    pointer,
                    -1,
                    destination,
                    length
                ) != length) return null;

            return new string(output, 0, length - 1);
        }
    }
}
