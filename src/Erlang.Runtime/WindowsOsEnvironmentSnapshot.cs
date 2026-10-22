using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class WindowsOsEnvironmentSnapshot
{
    internal static IReadOnlyList<KeyValuePair<string, string>> Capture()
    {
        nint address = GetEnvironmentStringsW();
        if (address == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        try
        {
            var result = new List<KeyValuePair<string, string>>();
            char* next = (char*)address;
            while (*next != '\0')
            {
                string entry = new(next);
                int separator = entry.IndexOf('=', entry[0] == '=' ? 1 : 0);
                if (separator > 0) result.Add(new(entry[..separator], entry[(separator + 1)..]));
                next += entry.Length + 1;
            }

            return result.AsReadOnly();
        }
        finally
        {
            _ = FreeEnvironmentStringsW(address);
        }
    }

    [DllImport(OsNativeNames.WindowsLibrary, SetLastError = true, ExactSpelling = true)]
    private static extern nint GetEnvironmentStringsW();

    [DllImport(OsNativeNames.WindowsLibrary, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeEnvironmentStringsW(nint block);
}
