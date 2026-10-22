using System.Runtime.InteropServices;

namespace Erlang;

// Unregistered host kernel. No guest ReferenceTerm identity or NIF body is claimed.
internal sealed class PrimTtyRedirectedHandleScope : IDisposable
{
    private readonly object gate = new();
    private readonly List<nint> ownedNullHandles = new();
    private nint input, output;
    private bool closed;
    internal bool CapturedUnavailable { get; private set; }

    private PrimTtyRedirectedHandleScope() { }

    internal static PrimTtyRedirectedHandleScope Create(bool standardOutput)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();
        var result = new PrimTtyRedirectedHandleScope();
        try
        {
            result.input = standardOutput ? PrimTtyWindowsNative.GetStdHandle(PrimTtyWindowsNames.StandardInput) : -1;
            if (standardOutput && Invalid(result.input))
                result.input = result.OpenNull(PrimTtyWindowsNames.GenericRead);
            result.output = PrimTtyWindowsNative.GetStdHandle(standardOutput
                ? PrimTtyWindowsNames.StandardOutput : PrimTtyWindowsNames.StandardError);
            if (Invalid(result.output))
                result.output = result.OpenNull(PrimTtyWindowsNames.GenericWrite);
            // Refuse BEFORE any SetConsoleMode/SetConsoleOutputCP mutation.
            if (PrimTtyWindowsNative.GetConsoleMode(result.output, out _)
                || PrimTtyWindowsNative.GetConsoleMode(result.input, out _))
                throw new NotSupportedException(PrimTtyWindowsNames.ConsoleRefusal);
            result.CapturedUnavailable = true;

            return result;
        }
        catch (Exception original)
        {
            try
            {
                result.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    private nint OpenNull(uint access)
    {
        nint handle = PrimTtyWindowsNative.CreateFileW(
            PrimTtyWindowsNames.NullDevice,
            access,
            0,
            0,
            PrimTtyWindowsNames.OpenExisting,
            PrimTtyWindowsNames.NormalAttributes,
            0
        );
        if (!Invalid(handle))
            ownedNullHandles.Add(handle);

        return handle;
    }

    private static bool Invalid(nint handle) => handle == 0 || handle == -1;

    // Pin tty_encoding: char input uses utf16/little, all other kinds use utf8.
    internal bool InputIsUtf16Little()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            return PrimTtyWindowsNative.GetFileType(input) == PrimTtyWindowsNames.TypeCharacter;
        }
    }

    internal (int Width, int Height, PrimTtyWindowsError? Error) WindowSize()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            var info = new PrimTtyWindowsNative.ScreenInfo
            {
                Size = (uint)Marshal.SizeOf<PrimTtyWindowsNative.ScreenInfo>(),
                Colors = new uint[16]
            };
            if (!PrimTtyWindowsNative.GetConsoleScreenBufferInfoEx(output, ref info))
                return (0, 0, new(nameof(PrimTtyWindowsNative.GetConsoleScreenBufferInfoEx),
                    (uint)Marshal.GetLastPInvokeError()));

            return (info.BufferSize.X, info.BufferSize.Y, null);
        }
    }

    internal static (bool? IsTerminal, bool BadDescriptor) IsTerminal(int standardHandle)
    {
        nint handle = PrimTtyWindowsNative.GetStdHandle(standardHandle);
        if (Invalid(handle))
            return (null, true);

        return PrimTtyWindowsNative.GetFileType(handle) switch
        {
            PrimTtyWindowsNames.TypeCharacter => (true, false),
            PrimTtyWindowsNames.TypeDisk or PrimTtyWindowsNames.TypePipe => (false, false),
            _ => (null, true)
        };
    }

    internal (bool? Open, bool BadArgument, PrimTtyWindowsError? Error) IsOpen(int standardHandle)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            nint handle = PrimTtyWindowsNative.GetStdHandle(standardHandle);
            if (Invalid(handle))
                return (null, true, null);
            switch (PrimTtyWindowsNative.GetFileType(handle))
            {
                case PrimTtyWindowsNames.TypeCharacter:
                    return (PrimTtyWindowsNative.GetNumberOfConsoleInputEvents(handle, out _), false, null);
                case PrimTtyWindowsNames.TypeDisk:
                    return (true, false, null);
                default:
                    if (PrimTtyWindowsNative.PeekNamedPipe(
                        handle,
                        0,
                        0,
                        0,
                        out _,
                        0
                    ))
                        return (true, false, null);
                    uint error = (uint)Marshal.GetLastPInvokeError();

                    return error == PrimTtyWindowsNames.BrokenPipe ? (false, false, null)
                        : (null, false, new(nameof(PrimTtyWindowsNative.PeekNamedPipe), error));
            }
        }
    }

    internal nint InputHandle
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, this);

                return input;
            }
        }
    }
    internal nint OutputHandle
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, this);

                return output;
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
            List<Exception>? failures = null;
            foreach (nint handle in ownedNullHandles)
                if (!PrimTtyWindowsNative.CloseHandle(handle))
                    (failures ??= new()).Add(new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()));
            ownedNullHandles.Clear();
            input = output = -1;
            if (failures is not null)
                throw new AggregateException(failures);
        }
    }
}
