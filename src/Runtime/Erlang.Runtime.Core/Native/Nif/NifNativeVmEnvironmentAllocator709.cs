using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class NifNativeVmEnvironmentAllocator709
{
    private readonly ProcessRuntime runtime;
    // Rooted by the VM, never by the newest native code image.
    private readonly NifNativeEnvironmentAllocator allocator;
    internal nint Pointer { get; }

    internal NifNativeVmEnvironmentAllocator709(ProcessRuntime runtime)
    {
        this.runtime = runtime;
        allocator = Allocate;
        Pointer = Marshal.GetFunctionPointerForDelegate(allocator);
    }

    private nint Allocate()
    {
        try
        {
            return NifNativeCallEnvironment.AllocateIndependent709(runtime).Pointer;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
