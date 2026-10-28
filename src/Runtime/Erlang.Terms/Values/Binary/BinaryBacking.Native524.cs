namespace Erlang;

internal sealed partial class BinaryBacking
{
    private readonly INativeBinaryStorage524? native524;
    internal INativeBinaryStorage524? NativeStorage524 => native524;

    internal BinaryBacking(INativeBinaryStorage524 native)
    {
        ArgumentNullException.ThrowIfNull(native);
        native524 = native;
        buffer = [];
        IsReference = true;
        apparentBytes = native.DataLength524;
    }
}
