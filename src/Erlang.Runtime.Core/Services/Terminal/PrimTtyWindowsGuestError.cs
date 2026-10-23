using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal static class PrimTtyWindowsGuestError
{
    internal static Term Error(uint code, string? operation = null)
    {
        nint buffer = 0;
        try
        {
            uint count = PrimTtyWindowsNative.FormatMessageA(
                PrimTtyNames.MessageFlags,
                0,
                code,
                0,
                out buffer,
                0,
                0
            );
            if (count == 0 || buffer == 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
            byte[] bytes = new byte[checked((int)count)];
            Marshal.Copy(
                buffer,
                bytes,
                0,
                bytes.Length
            );
            Term message = Term.A(Encoding.Latin1.GetString(bytes));

            return PrimTtyAtoms.Failure(operation is null ? message : Term.Tuple(Term.A(operation), message));
        }
        finally
        {
            if (buffer != 0)
                PrimTtyWindowsNative.LocalFree(buffer);
        }
    }
}
