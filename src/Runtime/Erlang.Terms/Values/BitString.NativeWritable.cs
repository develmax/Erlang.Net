namespace Erlang;

public sealed partial class BitString
{
    // Snapshot is prepared only by the reviewed actual guest importer. It grants
    // no execution permission and never changes the source writer or capacity.
    internal WritableBinarySnapshot CaptureWritableConstruction() => backing.CaptureWritable(writer);
}
