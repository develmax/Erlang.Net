using System.Reflection;

namespace Erlang.NifNativeBackend;

internal static class HeapBitStringTestAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly PropertyInfo Heap = typeof(BitString).GetProperty(HeapBitStringCaseNames.HeapStorage, Members)!;
    private static readonly PropertyInfo Resource = typeof(BitString).GetProperty(HeapBitStringCaseNames.ResourceStorage, Members)!;
    private static readonly MethodInfo ReadBit = typeof(BitString).GetMethod(HeapBitStringCaseNames.ReadBit, Members)!;

    private static readonly MethodInfo ReferenceFactory = typeof(BitString).GetMethod(HeapBitStringCaseNames.ReferenceFactory, BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly PropertyInfo SubBits = typeof(BitString).GetProperty(HeapBitStringCaseNames.HasSubBits, Members)!;
    private static readonly PropertyInfo Referenced = typeof(BitString).GetProperty(HeapBitStringCaseNames.ReferencedBytes, Members)!;

    internal static BitString ReferenceView(byte[] parent, int offset, int bits) => (BitString)ReferenceFactory.Invoke(null, [parent, offset, bits])!;

    internal static bool HasSubBits(BitString value) => (bool)SubBits.GetValue(value)!;

    internal static int ReferencedBytes(BitString value) => (int)Referenced.GetValue(value)!;

    internal static object? Storage(BitString value) => Heap.GetValue(value);

    internal static object? ResourceStorage(BitString value) => Resource.GetValue(value);

    internal static int Bit(BitString value, int index) => (int)ReadBit.Invoke(value, [index])!;
}
