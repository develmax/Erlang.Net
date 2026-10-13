namespace Erlang;

internal sealed record SequenceTraceToken(
    SequenceTraceFlags Flags,
    Term Label,
    long Serial,
    Term Sender,
    long LastCount
)
{
    public Term ToTerm() => Term.Tuple(
        Term.I((int)Flags),
        Label,
        Term.I(Serial),
        Sender,
        Term.I(LastCount)
    );
}
