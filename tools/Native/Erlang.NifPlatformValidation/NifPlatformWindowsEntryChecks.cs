using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

internal static unsafe class NifPlatformWindowsEntryChecks
{
    internal static void Run(string path)
    {
        using var callbacks = new NifNativeWindowsCallbacks();
        nint library = NativeLibrary.Load(Path.GetFullPath(path));
        try
        {
            var initialize = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(library, "nif_init");
            NifNativeAbiEntry entry = NifNativeEntryReader.Read(initialize(callbacks.Pointer), "nif_platform_fixture");
            if (Marshal.PtrToStringUTF8(entry.Name) != "nif_platform_fixture" || entry.FunctionCount != 3 || entry.ResourceInitSize != (nuint)Marshal.SizeOf<NativeResourceCallbacks527>())
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
            string[] names = ["sum", "tuple", "generation"];
            for (int index = 0; index < entry.FunctionCount; index++)
            {
                var function = Marshal.PtrToStructure<NifNativeAbiFunction>(entry.Functions + index * NifNativeAbi.FunctionBytes);
                if (Marshal.PtrToStringUTF8(function.Name) != names[index] || function.Arity != (index == 0 ? 2 : 0) || function.Function == 0)
                    throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Passed = true,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                PointerBytes = IntPtr.Size,
                NativeInitializerExecuted = true,
                NativeLoadCallbackExecuted = false,
                FullRuntimeNifExecuted = false,
                EntryBytes = Marshal.SizeOf<NifNativeAbiEntry>(),
                ResourceInitSize = (ulong)entry.ResourceInitSize,
                entry.FunctionCount
            }));
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }
}
