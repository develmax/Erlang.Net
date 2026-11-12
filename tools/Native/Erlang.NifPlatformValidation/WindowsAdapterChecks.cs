using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Erlang;

internal static unsafe class WindowsAdapterChecks
{
    private static int count;

    internal static void VerifyRuntimeBinding()
    {
        nint native = NifWindowsVariadic526.SetOption;
        if (native == 0)
            throw new InvalidOperationException("The selected runtime asset has no native set_option adapter");
        nint* slots = stackalloc nint[NifNativeWindowsCallbackLayout.FunctionCount + NifNativeWindowsCallbackLayout.TailPointers];
        NifNativeOptional793Registration.InstallWindows(slots);
        if (slots[NifNativeOptional793Names.SetOptionSlot] != native
            || ((delegate* unmanaged[Cdecl]<nint, int, int>)native)(0, 1) != NifNativeOptional793Names.InvalidArgument)
            throw new InvalidOperationException("The runtime did not bind the native variadic set_option slot");
    }

    internal static void Run(string path)
    {
        nint library = NativeLibrary.Load(path);
        try
        {
            var register = (delegate* unmanaged[Cdecl]<NifWindowsVariadic526Table*, int>)NativeLibrary.GetExport(library, NifWindowsVariadic526Names.Register);
            var table = new NifWindowsVariadic526Table
            {
                Version = NifWindowsVariadic526Names.Version,
                Size = (uint)sizeof(NifWindowsVariadic526Table),
                MaximumTuple = 32,
                MaximumList = 32,
                Tuple = &Collect,
                List = &Collect,
                SystemLimit = &Limit,
                FormatTerm791 = &Format
            };
            if (register(&table) != 0 || register(&table) != 0)
                throw new InvalidOperationException("Native registration failed");
            var setOption = (delegate* unmanaged[Cdecl]<nint, int, int>)NativeLibrary.GetExport(library, NifWindowsVariadic526Names.SetOption);
            if (setOption(0, 1) != NifNativeOptional793Names.InvalidArgument)
                throw new InvalidOperationException("Unsupported option was accepted");
            // Empty variadic calls contain only named arguments on both Windows ABIs.
            var tuple = (delegate* unmanaged[Cdecl]<nint, uint, nuint>)NativeLibrary.GetExport(library, NifWindowsVariadic526Names.Tuple);
            if (tuple(0, 0) != 0 || count != 0 || tuple(0, 33) != nuint.MaxValue)
                throw new InvalidOperationException("Variadic empty/bounded tuple contract failed");
            table.MaximumTuple = 1;
            if (register(&table) == 0)
                throw new InvalidOperationException("Native callback replacement was admitted");
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nuint Collect(nint environment, nuint* values, uint length)
    {
        count = (int)length;

        return length;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nuint Limit(nint environment) => nuint.MaxValue;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Format(
        nuint term,
        long depth,
        nint writer,
        nint state
    ) => 0;
}
