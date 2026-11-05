namespace Erlang;

internal sealed class DetsFileException : IOException
{
    internal DetsFileException(string reason, string? location = null) : base(reason)
    {
        Reason = reason;
        Location = location;
    }

    public string Reason { get; }
    public string? Location { get; }
}
