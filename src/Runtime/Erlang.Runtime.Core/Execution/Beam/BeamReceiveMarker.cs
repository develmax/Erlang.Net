namespace Erlang;

internal sealed class BeamReceiveMarker
{
    internal BeamReceiveMarker(Mailbox owner)
    {
        Owner = owner;
    }

    internal Mailbox Owner { get; }
}
