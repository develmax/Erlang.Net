using System.Runtime.InteropServices;
namespace Erlang;

// Child-private allocation/library owner. The complete104 callback table must
// retain this SafeHandle until every driver image and physical worker exits.
internal sealed unsafe partial class ErlDriverPrimitiveServices422 : SafeHandle
{
    private readonly object gate = new();
    private readonly Dictionary<nint, nuint> raw = new();
    private readonly Dictionary<nint, ErlDriverBinaryAllocation422> binaries = new();
    private readonly Dictionary<nint, int> libraries = new();
    private readonly IErlDriverPrimitiveHost422 host;
    private readonly Func<bool> schedulerThread;
    private readonly Action<Exception> report;
    private readonly Dictionary<string, int> layout;
    private bool closed;
    private int releaseWindowsError;
    private Exception? failure, reportingFailure;
    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }
    internal Exception? Failure => failure;
    internal Exception? ReportingFailure => reportingFailure;
    internal int ReleaseWindowsError => releaseWindowsError;
    internal int OwnedAllocationCount
    {
        get
        {
            lock (gate) return checked(raw.Count + binaries.Count + libraries.Values.Sum());
        }
    }

    internal ErlDriverPrimitiveServices422(
        IReadOnlyDictionary<string, int> measuredLayout,
        IReadOnlyDictionary<int, string> nativeErrnoNames,
        IErlDriverPrimitiveHost422 host,
        Func<bool> schedulerThread,
        Action<Exception> report
    ) : base(nint.Zero, true)
    {
        this.host = host;
        this.schedulerThread = schedulerThread;
        this.report = report;
        errnoNames = new Dictionary<int, string>(nativeErrnoNames);
        layout = new(measuredLayout);
        if (!OperatingSystem.IsWindows() || IntPtr.Size != 8 || Field(ErlDriverLayoutNames422.BinaryLength) != 0
            || Field(ErlDriverLayoutNames422.BinaryData) != 8 || Field(ErlDriverLayoutNames422.BinaryWidth) != 8
            || Field(ErlDriverLayoutNames422.IoLengthWidth) != 4)
            throw new PlatformNotSupportedException(ErlDriverPrimitiveNames422.InvalidLayout);
        void* owner = NativeMemory.AllocZeroed(4 * sizeof(ulong));
        if (owner is null) throw new OutOfMemoryException();
        SetHandle((nint)owner);
        try
        {
            Callbacks = CreateCallbacks();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public override bool IsInvalid => handle == nint.Zero;

    private int Field(string name) => layout.TryGetValue(name, out int value) && value >= 0
        ? value : throw new InvalidDataException(ErlDriverPrimitiveNames422.InvalidLayout);

    private void Fault(Exception error)
    {
        Interlocked.CompareExchange(ref failure, error, null);
        try
        {
            report(error);
        }
        catch (Exception secondary)
        {
            Interlocked.CompareExchange(ref reportingFailure, secondary, null);
        }
    }

    private T Boundary<T>(Func<T> body, T failed, bool allocation = false)
    {
        try
        {
            return body();
        }
        catch (OutOfMemoryException) when (allocation)
        {
            return failed;
        }
        catch (OverflowException) when (allocation)
        {
            return failed;
        }
        catch (Exception error)
        {
            Fault(error);

            return failed;
        }
    }

    private void Boundary(Action body)
    {
        try
        {
            body();
        }
        catch (Exception error)
        {
            Fault(error);
        }
    }

    private IReadOnlyDictionary<string, Delegate> CreateCallbacks() => new Dictionary<string, Delegate>
    {
        [nameof(ErlDriverCallbackTypes420.null_func)] = new ErlDriverCallbackTypes420.null_func(static () => 0),
        [nameof(ErlDriverCallbackTypes420.driver_alloc)] = new ErlDriverCallbackTypes420.driver_alloc(size => Boundary(() => Allocate(size), nint.Zero, true)),
        [nameof(ErlDriverCallbackTypes420.driver_realloc)] = new ErlDriverCallbackTypes420.driver_realloc((pointer, size) => Boundary(() => Reallocate(pointer, size), nint.Zero, true)),
        [nameof(ErlDriverCallbackTypes420.driver_free)] = new ErlDriverCallbackTypes420.driver_free(pointer => Boundary(() => Free(pointer))),
        [nameof(ErlDriverCallbackTypes420.driver_alloc_binary)] = new ErlDriverCallbackTypes420.driver_alloc_binary(size => Boundary(() => AllocateBinary(size), nint.Zero, true)),
        [nameof(ErlDriverCallbackTypes420.driver_realloc_binary)] = new ErlDriverCallbackTypes420.driver_realloc_binary((pointer, size) => Boundary(() => ReallocateBinary(pointer, size), nint.Zero, true)),
        [nameof(ErlDriverCallbackTypes420.driver_free_binary)] = new ErlDriverCallbackTypes420.driver_free_binary(pointer => Boundary(() => ReleaseBinary(pointer))),
        [nameof(ErlDriverCallbackTypes420.driver_binary_get_refc)] = new ErlDriverCallbackTypes420.driver_binary_get_refc(pointer => Boundary(() => BinaryReferences(pointer, 0), (nint)(-1))),
        [nameof(ErlDriverCallbackTypes420.driver_binary_inc_refc)] = new ErlDriverCallbackTypes420.driver_binary_inc_refc(pointer => Boundary(() => BinaryReferences(pointer, 1), (nint)(-1))),
        [nameof(ErlDriverCallbackTypes420.driver_binary_dec_refc)] = new ErlDriverCallbackTypes420.driver_binary_dec_refc(pointer => Boundary(() => BinaryReferences(pointer, -1), (nint)(-1))),
        [nameof(ErlDriverCallbackTypes420.driver_vec_to_buf)] = new ErlDriverCallbackTypes420.driver_vec_to_buf((vector, buffer, length) => Boundary(() => VectorToBuffer(vector, buffer, length), (nuint)0)),
        [nameof(ErlDriverCallbackTypes420.erl_errno_id)] = new ErlDriverCallbackTypes420.erl_errno_id(error => Boundary(() => Errno(error), nint.Zero)),
        [nameof(ErlDriverCallbackTypes420.driver_dl_open)] = new ErlDriverCallbackTypes420.driver_dl_open(path => Boundary(() => OpenLibrary(path), nint.Zero, true)),
        [nameof(ErlDriverCallbackTypes420.driver_dl_sym)] = new ErlDriverCallbackTypes420.driver_dl_sym((image, name) => Boundary(() => FindSymbol(image, name), nint.Zero)),
        [nameof(ErlDriverCallbackTypes420.driver_dl_close)] = new ErlDriverCallbackTypes420.driver_dl_close(image => Boundary(() => CloseLibrary(image), -1)),
        [nameof(ErlDriverCallbackTypes420.driver_dl_error)] = new ErlDriverCallbackTypes420.driver_dl_error(() => Boundary(LibraryError, nint.Zero)),
        [nameof(ErlDriverCallbackTypes420.driver_system_info)] = new ErlDriverCallbackTypes420.driver_system_info((buffer, length) => Boundary(() => SystemInfo(buffer, length))),
        [nameof(ErlDriverCallbackTypes420.driver_get_now)] = new ErlDriverCallbackTypes420.driver_get_now(buffer => Boundary(() => GetNow(buffer), -1)),
        [nameof(ErlDriverCallbackTypes420.erl_drv_monotonic_time)] = new ErlDriverCallbackTypes420.erl_drv_monotonic_time(unit => Boundary(() => ReadClock(false, unit), ErlDriverPrimitiveNames422.TimeError)),
        [nameof(ErlDriverCallbackTypes420.erl_drv_time_offset)] = new ErlDriverCallbackTypes420.erl_drv_time_offset(unit => Boundary(() => ReadClock(true, unit), ErlDriverPrimitiveNames422.TimeError)),
        [nameof(ErlDriverCallbackTypes420.erl_drv_convert_time_unit)] = new ErlDriverCallbackTypes420.erl_drv_convert_time_unit((value, from, to) => ConvertTime(value, from, to)),
        [nameof(ErlDriverCallbackTypes420.erl_drv_putenv)] = new ErlDriverCallbackTypes420.erl_drv_putenv((key, value) => Boundary(() => host.WriteEnvironment(CString(key), CString(value)), -1)),
        [nameof(ErlDriverCallbackTypes420.erl_drv_getenv)] = new ErlDriverCallbackTypes420.erl_drv_getenv((key, value, length) => Boundary(() => GetEnvironment(key, value, length), -1))
    };

    private void RequireOpen() => ObjectDisposedException.ThrowIf(closed, this);

    private void Census()
    {
        // Actual owner-control storage, not a fabricated public VM heap metric.
        var words = (ulong*)handle;
        words[0] = (ulong)raw.Count;
        words[1] = (ulong)binaries.Count;
        ulong count = 0;
        foreach (int references in libraries.Values) count += (uint)references;
        words[2] = count;
    }

    protected override bool ReleaseHandle()
    {
        lock (gate)
        {
            closed = true;
            foreach (var library in libraries)
                for (int index = 0; index < library.Value; index++)
                    if (!FreeLibrary(library.Key)) Interlocked.CompareExchange(ref releaseWindowsError, Marshal.GetLastPInvokeError(), 0);
            libraries.Clear();
            foreach (nint pointer in binaries.Keys) NativeMemory.Free((void*)pointer);
            binaries.Clear();
            foreach (nint pointer in raw.Keys) NativeMemory.Free((void*)pointer);
            raw.Clear();
            symbolError.Dispose();
            errorBuffer.Dispose();
            permanentStrings.Clear();
            errorNames.Clear();
            NativeMemory.Free((void*)handle);
        }

        return failure is null && releaseWindowsError == 0;
    }
}
