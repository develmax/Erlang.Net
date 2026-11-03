namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // A typed private token is mutable metadata, even when its first payload
    // happens to contain only small integers and atoms. Never certify it.
    internal TupleTerm CreateMutableSequenceToken550(SequenceTraceToken token)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        SequenceTraceToken snapshot = SequenceTraceToken.FromPortable((TupleTerm)token.ToTerm());
        Term[] children =
        [
            Term.I((long)snapshot.Flags),
            snapshot.Label,
            Term.I(snapshot.Serial),
            snapshot.Sender,
            Term.I(snapshot.LastCount)
        ];

        return new TupleTerm(AllocateEncoded526(
            children,
            [EncodedMapHeader526.Tuple(SequenceTraceRootNames.Arity)],
            immutablePlainClosure550: false,
            mutableSequenceToken814: true
        ));
    }
}
