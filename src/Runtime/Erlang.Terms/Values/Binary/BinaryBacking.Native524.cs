namespace Erlang;

internal sealed partial class BinaryBacking
{
    private readonly INativeBinaryStorage524? native524;
    internal INativeBinaryStorage524? NativeStorage524 => native524;

    // A consumed copy does not publish the native pointer or backing to another
    // owner. Keep the allocation live without revoking the actual append writer.
    internal IDisposable? RetainNativeCopyRead524() => native524 is INativeWritableBinaryStorage writable
        ? writable.RetainCopyRead() : native524?.RetainRoot524();

    internal byte ReadByte524(int index) => native524 is INativeWritableBinaryStorage writable
        ? writable.ReadWritableByte(index) : ReadSpan524[index];

    internal BinaryBacking(INativeBinaryStorage524 native)
    {
        ArgumentNullException.ThrowIfNull(native);
        native524 = native;
        buffer = [];
        IsReference = true;
        apparentBytes = native.DataLength524;
    }
}
