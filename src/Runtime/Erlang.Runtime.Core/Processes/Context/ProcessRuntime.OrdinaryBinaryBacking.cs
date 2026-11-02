namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeOrdinaryBinaryOwner ordinaryBinaryBackings = new();
    internal bool OrdinaryBinaryAdmissionClosed => ordinaryBinaryBackings.AdmissionClosed;

    internal OrdinaryBinaryBacking CaptureOrdinaryBinary(BinaryBacking source)
    {
        ThrowIfHalted();

        return OrdinaryBinaryBacking.Capture(this, source, ordinaryBinaryBackings.Reserve());
    }

    internal OrdinaryBinaryBacking CaptureWritableBinary(WritableBinarySnapshot source)
    {
        ThrowIfHalted();

        return OrdinaryBinaryBacking.CaptureWritable(this, source, ordinaryBinaryBackings.Reserve());
    }

    private void StopOrdinaryBinaryAdmission() => ordinaryBinaryBackings.Stop();

    private Task DrainOrdinaryBinaries() => ordinaryBinaryBackings.Drain();
}
