namespace Erlang;

// Live token fields come from an actual six-word heap tuple. Detached metadata
// needs an independent transport owner when it outlives the current operation.
internal sealed record SequenceTraceToken
{
    private TupleTerm? storage;
    private SequenceTraceFlags flags;
    private Term label = Nil.Value;
    private long serial;
    private Term sender = Nil.Value;
    private long lastCount;

    public SequenceTraceFlags Flags
    {
        get => storage is null ? flags : (SequenceTraceFlags)(long)((Integer)storage.Items[SequenceTraceRootNames.Flags]).Value;
        init
        {
            Detach();
            flags = value;
        }
    }

    public Term Label
    {
        get => storage?.Items[SequenceTraceRootNames.Label] ?? label;
        init
        {
            Detach();
            label = value;
        }
    }

    public long Serial
    {
        get => storage is null ? serial : (long)((Integer)storage.Items[SequenceTraceRootNames.Serial]).Value;
        init
        {
            Detach();
            serial = value;
        }
    }

    public Term Sender
    {
        get => storage?.Items[SequenceTraceRootNames.Sender] ?? sender;
        init
        {
            Detach();
            sender = value;
        }
    }

    public long LastCount
    {
        get => storage is null ? lastCount : (long)((Integer)storage.Items[SequenceTraceRootNames.LastCount]).Value;
        init
        {
            Detach();
            lastCount = value;
        }
    }

    internal TupleTerm? PhysicalTuple => storage;

    public SequenceTraceToken(
        SequenceTraceFlags Flags,
        Term Label,
        long Serial,
        Term Sender,
        long LastCount
    )
    {
        flags = Flags;
        label = Label;
        serial = Serial;
        sender = Sender;
        lastCount = LastCount;
    }

    private SequenceTraceToken(TupleTerm tuple) => storage = tuple;

    internal static SequenceTraceToken FromPhysical(TupleTerm tuple)
    {
        Validate(tuple);
        if (tuple.OwnedStorage506 is null)
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);

        return new(tuple);
    }

    internal static SequenceTraceToken FromPortable(TupleTerm tuple)
    {
        Validate(tuple);

        return new(
            (SequenceTraceFlags)(long)((Integer)tuple.Items[SequenceTraceRootNames.Flags]).Value,
            tuple.Items[SequenceTraceRootNames.Label],
            (long)((Integer)tuple.Items[SequenceTraceRootNames.Serial]).Value,
            tuple.Items[SequenceTraceRootNames.Sender],
            (long)((Integer)tuple.Items[SequenceTraceRootNames.LastCount]).Value
        );
    }

    private static void Validate(TupleTerm tuple)
    {
        if (tuple.Items.Count != SequenceTraceRootNames.Arity ||
            tuple.Items[SequenceTraceRootNames.Flags] is not Integer flags ||
            tuple.Items[SequenceTraceRootNames.Serial] is not Integer serial ||
            tuple.Items[SequenceTraceRootNames.LastCount] is not Integer last ||
            tuple.Items[SequenceTraceRootNames.Sender] is not (Pid or Atom) ||
            !Small(flags) || !Small(serial) || !Small(last))
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
    }

    private static bool Small(Integer value)
        => value.Value >= SequenceTraceParameters.MinimumSerial && value.Value <= SequenceTraceParameters.MaximumSerial;

    private void Detach()
    {
        if (storage is null)
            return;
        SequenceTraceFlags oldFlags = Flags;
        Term oldLabel = Label;
        long oldSerial = Serial;
        Term oldSender = Sender;
        long oldLast = LastCount;
        flags = oldFlags;
        label = oldLabel;
        serial = oldSerial;
        sender = oldSender;
        lastCount = oldLast;
        storage = null;
    }

    // The mutable internal tuple must never escape to language callers.
    public Term ToTerm() => Term.Tuple(
        Term.I((long)Flags),
        Label,
        Term.I(Serial),
        Sender,
        Term.I(LastCount)
    );
}
