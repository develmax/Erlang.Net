namespace Erlang;

internal sealed class ErlDriverSelectRow420
{
    internal ErlDriverSelectRow420(nint handle) => Handle = handle;

    internal nint Handle { get; }
    internal ErlDriverPort420? Reader { get; set; }
    internal ErlDriverPort420? Writer { get; set; }
    internal ErlDriverPort420? User { get; set; }
    internal ErlDriverImageLease420? ReaderImage { get; set; }
    internal ErlDriverImageLease420? WriterImage { get; set; }
    internal ErlDriverImageLease420? UserImage { get; set; }
    internal bool StopPending { get; set; }
    internal ulong StopDriver { get; set; }
    internal ulong ReadGeneration { get; set; }
    internal ulong WriteGeneration { get; set; }
    internal bool ReadPending { get; set; }
    internal bool WritePending { get; set; }
    internal ulong Generation { get; set; }
    internal ErlDriverSelection420? Wait { get; set; }
    internal List<Task> Pending { get; } = new();
}
