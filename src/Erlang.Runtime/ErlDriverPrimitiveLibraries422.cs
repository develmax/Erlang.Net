using System.Runtime.InteropServices;
using System.Text;
namespace Erlang;

internal sealed unsafe partial class ErlDriverPrimitiveServices422
{
    private readonly ThreadLocal<int> symbolError = new();
    private readonly ThreadLocal<nint> errorBuffer = new();
    private readonly Dictionary<string, nint> permanentStrings = new(StringComparer.Ordinal);
    private readonly Dictionary<int, nint> errorNames = new();
    private IReadOnlyDictionary<int, string> errnoNames = null!;

    [DllImport(ErlDriverPrimitiveNames422.Kernel, EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibrary(string path, nint reserved, uint flags);

    [DllImport(ErlDriverPrimitiveNames422.Kernel, EntryPoint = "FreeLibrary", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(nint image);

    [DllImport(ErlDriverPrimitiveNames422.Kernel, EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern nint FindExport(nint image, byte[] name);

    [DllImport(ErlDriverPrimitiveNames422.Kernel, EntryPoint = "FormatMessageA", SetLastError = true)]
    private static extern uint FormatMessage(
        uint flags,
        nint source,
        uint message,
        uint language,
        nint buffer,
        uint size,
        nint arguments
    );

    private nint PermanentString(string value)
    {
        lock (gate)
        {
            RequireOpen();
            if (permanentStrings.TryGetValue(value, out var existing)) return existing;
            permanentStrings.EnsureCapacity(checked(permanentStrings.Count + 1));
            nint pointer = CopyCString(Encoding.UTF8.GetBytes(value));
            permanentStrings.Add(value, pointer);

            return pointer;
        }
    }

    private nint Errno(int error)
    {
        lock (gate)
        {
            RequireOpen();
            if (errorNames.TryGetValue(error, out var existing)) return existing;
            errorNames.EnsureCapacity(checked(errorNames.Count + 1));
            string text = errnoNames.TryGetValue(error, out string? actual) ? actual :
                ErlDriverPrimitiveNames422.ErrnoPrefix + error.ToString(System.Globalization.CultureInfo.InvariantCulture);
            nint pointer = PermanentString(text);
            errorNames.Add(error, pointer);

            return pointer;
        }
    }

    private nint OpenLibrary(nint path)
    {
        byte[] bytes = CString(path);
        if (bytes.Length >= ErlDriverPrimitiveNames422.MaximumPath - ErlDriverPrimitiveNames422.Extension.Length) return nint.Zero;
        string windows = ErlDriverPath420.FromWire(bytes).WindowsPath + ErlDriverPrimitiveNames422.Extension;
        lock (gate)
        {
            RequireOpen();
            libraries.EnsureCapacity(checked(libraries.Count + 1));
            nint module = LoadLibrary(windows, nint.Zero, ErlDriverPrimitiveNames422.AlteredSearchPath);
            // Pinned io.c uses different TLS keys for open and sym/error.
            // A failed open therefore does not replace the visible sym error.
            if (module == nint.Zero) return module;
            libraries[module] = checked(libraries.GetValueOrDefault(module) + 1);
            Census();

            return module;
        }
    }

    private nint FindSymbol(nint image, nint name)
    {
        byte[] text = [.. CString(name), 0];
        lock (gate)
        {
            RequireOpen();
            nint address = FindExport(image, text);
            if (address == nint.Zero) symbolError.Value = checked(ErlDriverPrimitiveNames422.DynamicOffset - Marshal.GetLastPInvokeError());

            return address;
        }
    }

    private int CloseLibrary(nint image)
    {
        lock (gate)
        {
            RequireOpen();
            if (!FreeLibrary(image)) return checked(ErlDriverPrimitiveNames422.DynamicOffset - Marshal.GetLastPInvokeError());
            if (libraries.TryGetValue(image, out int references))
            {
                if (references == 1) libraries.Remove(image); else libraries[image] = references - 1;
                Census();
            }

            return 0;
        }
    }

    private nint LibraryError()
    {
        int error = symbolError.Value;
        if (error > ErlDriverPrimitiveNames422.DynamicOffset) return PermanentString(ErlDriverPrimitiveNames422.Unspecified);
        nint buffer = errorBuffer.Value;
        if (buffer == nint.Zero)
        {
            buffer = Allocate(ErlDriverPrimitiveNames422.MaximumError);
            if (buffer == nint.Zero) throw new OutOfMemoryException();
            errorBuffer.Value = buffer;
        }
        uint size = FormatMessage(
            ErlDriverPrimitiveNames422.FormatFromSystem,
            nint.Zero,
            checked((uint)(ErlDriverPrimitiveNames422.DynamicOffset - error)),
            0,
            buffer,
            ErlDriverPrimitiveNames422.MaximumError,
            nint.Zero
        );
        if (size == 0) return PermanentString(ErlDriverPrimitiveNames422.Unspecified);
        while (size > 0 && ((byte*)buffer)[size - 1] is (byte)'\r' or (byte)'\n') size--;
        ((byte*)buffer)[size] = 0;

        return buffer;
    }
}
