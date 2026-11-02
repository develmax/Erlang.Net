using System.Reflection;

namespace Erlang.NifNativeBackend;

internal static class OrdinaryBinaryTestAccess
{
    private const BindingFlags ReadMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static object? Storage(BitString value)
        => typeof(BitString).GetProperty(OrdinaryBinaryCaseNames.StorageProperty, ReadMembers)!.GetValue(value);

    internal static object? Owner(BitString value)
    {
        object backing = typeof(BitString).GetProperty(OrdinaryBinaryCaseNames.BackingProperty, ReadMembers)!.GetValue(value)!;

        return backing.GetType().GetProperty(OrdinaryBinaryCaseNames.NativeStorageProperty, ReadMembers)!.GetValue(backing);
    }

    internal static int ReferencedBytes(BitString value)
        => (int)typeof(BitString).GetProperty(OrdinaryBinaryCaseNames.ReferencedBytesProperty, ReadMembers)!.GetValue(value)!;
}
