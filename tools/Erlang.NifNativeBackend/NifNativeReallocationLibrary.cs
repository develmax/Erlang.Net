using System.Reflection;

namespace Erlang.NifNativeBackend;

internal sealed class NifNativeReallocationLibrary
{
    internal NifNativeLibraryOwner Owner
    {
        get;
    }
    internal NifNativeAllocationOwner Allocations
    {
        get;
    }
    internal object Gate
    {
        get;
    }
    internal bool Granted => (bool)typeof(NifNativeLibraryOwner).GetField(NifNativeReallocationHarnessNames.GrantField, NifNativeReallocationHarnessNames.Fields)!.GetValue(Owner)!;

    internal NifNativeReallocationLibrary(NifNativeLibraryOwner owner)
    {
        Owner = owner;
        Allocations = (NifNativeAllocationOwner)Field(NifNativeReallocationHarnessNames.AllocationsField).GetValue(owner)!;
        Gate = Field(NifNativeReallocationHarnessNames.GateField).GetValue(owner)!;
    }

    private static FieldInfo Field(string name) => typeof(NifNativeLibraryOwner).GetField(name, NifNativeReallocationHarnessNames.Fields)
        ?? throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
}
