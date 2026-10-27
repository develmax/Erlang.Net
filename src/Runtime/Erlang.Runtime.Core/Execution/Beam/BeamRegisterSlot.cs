namespace Erlang;

internal readonly struct BeamRegisterSlot
{
    private readonly object? value;

    private BeamRegisterSlot(object value) => this.value = value;

    internal object? StoredValue504 => value;

    internal Term RootGraph507() => Kind switch
    {
        BeamRegisterSlotKind.Term => ReadTerm(),
        BeamRegisterSlotKind.SavedTrace => Term.Tuple(ReadSavedTrace().Reason, ReadSavedTrace().StackTraceTerm),
        BeamRegisterSlotKind.BinaryMatchContext => ReadBinaryMatchContext().Source,
        BeamRegisterSlotKind.BinaryMatchPosition => ReadBinaryPosition().Source,
        // Handler checkpoints contain code/depth metadata, not hidden Terms.
        // Receive-marker references belong to the separate mailbox root domain.
        _ => Nil.Value
    };

    internal static BeamRegisterSlot FromStoredValue504(object? value) => value is null ? default : new(value);

    internal BeamRegisterSlotKind Kind => value switch
    {
        null => BeamRegisterSlotKind.Uninitialized,
        Term => BeamRegisterSlotKind.Term,
        BeamHandlerTag => BeamRegisterSlotKind.Handler,
        BeamSavedTrace => BeamRegisterSlotKind.SavedTrace,
        BeamReceiveMarker => BeamRegisterSlotKind.ReceiveMarker,
        BeamBinaryMatchContext => BeamRegisterSlotKind.BinaryMatchContext,
        BeamBinaryMatchPosition => BeamRegisterSlotKind.BinaryMatchPosition,
        _ => throw new InvalidDataException(BeamRegisterProtocolNames.TypedValue)
    };

    internal static BeamRegisterSlot FromTerm(Term value)
        => new(value ?? throw new ArgumentNullException(nameof(value)));

    internal static BeamRegisterSlot FromHandler(BeamHandlerTag value)
        => new(value ?? throw new ArgumentNullException(nameof(value)));

    internal static BeamRegisterSlot FromSavedTrace(BeamSavedTrace value)
        => new(value ?? throw new ArgumentNullException(nameof(value)));

    internal static BeamRegisterSlot FromReceiveMarker(BeamReceiveMarker value)
        => new(value ?? throw new ArgumentNullException(nameof(value)));

    internal static BeamRegisterSlot FromBinaryMatchContext(BeamBinaryMatchContext value)
        => new(value ?? throw new ArgumentNullException(nameof(value)));

    internal BeamBinaryMatchContext ReadBinaryMatchContext()
        => Kind == BeamRegisterSlotKind.BinaryMatchContext ? (BeamBinaryMatchContext)value!
            : throw new InvalidDataException(BeamBinaryNames.InvalidContext);

    internal static BeamRegisterSlot FromBinaryPosition(BeamBinaryMatchPosition value)
        => new(value ?? throw new ArgumentNullException(nameof(value)));

    internal BeamBinaryMatchPosition ReadBinaryPosition()
        => Kind == BeamRegisterSlotKind.BinaryMatchPosition ? (BeamBinaryMatchPosition)value!
            : throw new InvalidDataException(BeamBinaryNames.InvalidContext);

    internal BeamReceiveMarker ReadReceiveMarker()
        => Kind == BeamRegisterSlotKind.ReceiveMarker ? (BeamReceiveMarker)value!
            : throw new InvalidDataException(BeamReceiveMarkerNames.InvalidSlot);

    internal Term ReadTerm() => Kind switch
    {
        BeamRegisterSlotKind.Term => (Term)value!,
        BeamRegisterSlotKind.Uninitialized => throw new InvalidDataException(BeamRegisterNames.Uninitialized),
        _ => throw new InvalidDataException(BeamRegisterProtocolNames.TypedValue)
    };

    internal BeamHandlerTag ReadHandler()
        => Kind == BeamRegisterSlotKind.Handler ? (BeamHandlerTag)value!
            : throw new InvalidDataException(BeamRegisterProtocolNames.Handler);

    internal BeamSavedTrace ReadSavedTrace()
        => Kind == BeamRegisterSlotKind.SavedTrace ? (BeamSavedTrace)value!
            : throw new InvalidDataException(BeamRegisterProtocolNames.Trace);

    internal void RequireMovable()
    {
        if (Kind == BeamRegisterSlotKind.Uninitialized)
            throw new InvalidDataException(BeamRegisterNames.Uninitialized);
        if (Kind == BeamRegisterSlotKind.Handler)
            throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
    }
}
