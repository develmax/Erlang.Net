namespace Erlang;

internal sealed class ErlDriverAsyncJob420
{
    internal ErlDriverAsyncJob420(
        int identity,
        ErlDriverPort420 port,
        nint data,
        ErlDriverAsyncNativeTypes420.Work? free,
        ErlDriverImageLease420 image,
        ErlDriverPdl420? pdl
    )
    {
        Identity = identity;
        Port = port;
        Data = data;
        Free = free;
        Image = image;
        Pdl = pdl;
    }

    internal int Identity { get; }
    internal ErlDriverPort420 Port { get; }
    internal nint Data { get; }
    internal ErlDriverAsyncNativeTypes420.Work? Free { get; }
    internal ErlDriverImageLease420 Image { get; }
    internal ErlDriverPdl420? Pdl { get; }
    internal TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Completed;
    internal int Cleaned;
}
