using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Erlang;

internal static unsafe class NifWindowsVariadic526
{
    private static readonly object Gate = new();
    private static nint library;
    private static nint tuple;
    private static nint list;
    private static nint snprintf791;

    internal static nint Snprintf791
    {
        get
        {
            _ = EnsureRegistered();

            return snprintf791;
        }
    }

    internal static (nint Tuple, nint List) EnsureRegistered()
    {
        if (AssemblyLoadContext.GetLoadContext(typeof(NifWindowsVariadic526).Assembly)?.IsCollectible == true)
            throw new NotSupportedException(NifNativeDiagnostics.BridgeLifetime);
        lock (Gate)
        {
            if (library != 0)
                return (tuple, list);
            string directory = Path.GetDirectoryName(typeof(ProcessRuntime).Assembly.Location) ?? AppContext.BaseDirectory;
            string path = Path.Combine(directory, NifWindowsVariadic526Names.LibraryFile);
            nint candidate;
            try
            {
                candidate = File.Exists(path)
                    ? NativeLibrary.Load(path)
                    : NativeLibrary.Load(NifWindowsVariadic526Names.LibraryFile, typeof(ProcessRuntime).Assembly, null);
            }
            catch (DllNotFoundException error)
            {
                throw new DllNotFoundException(NifWindowsVariadic526Names.MissingAsset, error);
            }
            bool published = false;
            try
            {
                var register = (delegate* unmanaged[Cdecl]<NifWindowsVariadic526Table*, int>)NativeLibrary.GetExport(candidate, NifWindowsVariadic526Names.Register);
                nint candidateTuple = NativeLibrary.GetExport(candidate, NifWindowsVariadic526Names.Tuple);
                nint candidateList = NativeLibrary.GetExport(candidate, NifWindowsVariadic526Names.List);
                nint candidateSnprintf791 = NativeLibrary.GetExport(candidate, NifWindowsVariadic526Names.Snprintf791);
                var table = new NifWindowsVariadic526Table
                {
                    Version = NifWindowsVariadic526Names.Version,
                    Size = (uint)sizeof(NifWindowsVariadic526Table),
                    MaximumTuple = (uint)ErlangTupleLimits.MaximumArity,
                    MaximumList = (uint)Array.MaxLength,
                    Tuple = &NifNativeArrayCallbacks.MakeTuple,
                    List = &NifNativeArrayCallbacks.MakeList,
                    SystemLimit = &RaiseSystemLimit,
                    FormatTerm791 = &NifNativePrintf791Callbacks.RenderTerm
                };
                if (register(&table) != 0)
                    throw new InvalidOperationException(NifWindowsVariadic526Names.ForeignRegistration);
                // Native callbacks are process-owned and never replaced. Like
                // the existing Linux bridge, no image/env lifetime can free them.
                tuple = candidateTuple;
                list = candidateList;
                snprintf791 = candidateSnprintf791;
                library = candidate;
                published = true;

                return (tuple, list);
            }
            finally
            {
                if (!published)
                    NativeLibrary.Free(candidate);
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nuint RaiseSystemLimit(nint env)
    {
        try
        {
            NifNativeCallEnvironment.Resolve(env).Failure.Raise(NifNativeArrayNames.SystemLimit);
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);
        }

        return 0;
    }
}
