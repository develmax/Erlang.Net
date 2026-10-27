namespace Erlang;

public sealed partial class ProcessRuntime
{
    private ProcessBinaryRegistry512? binaryRegistry512;
    internal ProcessBinaryRegistry512 BinaryRegistry512 => LazyInitializer.EnsureInitialized(ref binaryRegistry512, () => new());
}
