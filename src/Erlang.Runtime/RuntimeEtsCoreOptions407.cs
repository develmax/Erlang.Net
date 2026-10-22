namespace Erlang;

internal sealed record RuntimeEtsCoreOptions407(
    EtsCoreTableKind407 Kind,
    RuntimeEtsProtection Protection,
    bool Named,
    long KeyPosition
)
{
    // Requested logical hints; the shared gate/leases retain actual storage safety.
    // These properties do not assert a physical native lock count or implement info/2.
    internal bool RequestedReadConcurrency { get; init; }
    internal EtsWriteConcurrency411 RequestedWriteConcurrency { get; init; }
    internal int RequestedFixedWriteLocks { get; init; }
    internal bool Compressed { get; init; }
    internal bool? RequestedDecentralizedCounters { get; init; }

    internal static RuntimeEtsCoreOptions407 Parse(Term options, CancellationToken cancellation)
    {
        var kind = EtsCoreTableKind407.Set;
        var protection = RuntimeEtsProtection.Protected;
        bool named = false;
        bool compressed = false;
        long keyPosition = 1;
        bool readConcurrency = false;
        var writeConcurrency = EtsWriteConcurrency411.Disabled;
        int fixedWriteLocks = 0;
        bool? decentralizedCounters = null;
        Term rest = options;
        while (rest is Cons cell)
        {
            cancellation.ThrowIfCancellationRequested();
            switch (cell.Head)
            {
                case Atom { Name: RuntimeEtsCoreNames407.Set or RuntimeEtsCoreNames407.Protected }: break; // source NO-OP, not resets
                case Atom { Name: RuntimeEtsCoreNames407.OrderedSet }: kind = EtsCoreTableKind407.OrderedSet; break;
                case Atom { Name: RuntimeEtsCoreNames407.Bag }: kind = EtsCoreTableKind407.Bag; break;
                case Atom { Name: RuntimeEtsCoreNames407.DuplicateBag }: kind = EtsCoreTableKind407.DuplicateBag; break;
                case Atom { Name: RuntimeEtsCoreNames407.Public }: protection = RuntimeEtsProtection.Public; break;
                case Atom { Name: RuntimeEtsCoreNames407.Private }: protection = RuntimeEtsProtection.Private; break;
                case Atom { Name: RuntimeEtsCoreNames407.Named }: named = true; break;
                case TupleTerm { Items.Count: 2 } pair when pair.Items[0] is Atom { Name: RuntimeEtsCoreNames407.KeyPosition }
                    && pair.Items[1] is Integer number && number.Value > 0 && number.Value <= RuntimeEtsCoreNames407.MaximumSmall64:
                    keyPosition = (long)number.Value; break;
                case TupleTerm { Items.Count: 2 } pair when pair.Items[0] is Atom { Name: RuntimeEtsCoreNames407.Heir }
                    && pair.Items[1] is Atom { Name: RuntimeEtsCoreNames407.None }:
                    break;
                case TupleTerm { Items.Count: 2 } pair when pair.Items[0] is Atom { Name: RuntimeEtsCoreNames407.ReadConcurrency }:
                    readConcurrency = pair.Items[1] switch
                    {
                        Atom { Name: ErlangBooleanAtoms.True } => true,
                        Atom { Name: ErlangBooleanAtoms.False } => false,
                        _ => throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None)
                    };
                    break;
                case TupleTerm { Items.Count: 2 } pair when pair.Items[0] is Atom { Name: RuntimeEtsCoreNames407.WriteConcurrency }:
                    fixedWriteLocks = 0;
                    writeConcurrency = pair.Items[1] switch
                    {
                        Atom { Name: ErlangBooleanAtoms.True } => EtsWriteConcurrency411.Enabled,
                        Atom { Name: ErlangBooleanAtoms.False } => EtsWriteConcurrency411.Disabled,
                        Atom { Name: EtsConcurrencyNames411.Auto } => EtsWriteConcurrency411.Automatic,
                        TupleTerm { Items.Count: 2 } fixedLocks
                            when fixedLocks.Items[0] is Atom { Name: EtsConcurrencyNames411.FixedHashLocks }
                                && fixedLocks.Items[1] is Integer count
                                && count.Value >= EtsConcurrencyNames411.MinimumFixedLocks
                                && count.Value <= EtsConcurrencyNames411.MaximumFixedLocks
                            => EtsWriteConcurrency411.FixedHashLocks,
                        _ => throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None)
                    };
                    if (writeConcurrency == EtsWriteConcurrency411.FixedHashLocks)
                        fixedWriteLocks = (int)((Integer)((TupleTerm)pair.Items[1]).Items[1]).Value;
                    break;
                case TupleTerm { Items.Count: 2 } pair when pair.Items[0] is Atom { Name: RuntimeEtsCoreNames407.Counters }:
                    decentralizedCounters = pair.Items[1] switch
                    {
                        Atom { Name: ErlangBooleanAtoms.True } => true,
                        Atom { Name: ErlangBooleanAtoms.False } => false,
                        _ => throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None)
                    };
                    break;
                case Atom { Name: RuntimeEtsCoreNames407.Compressed }:
                    compressed = true;
                    break;
                case TupleTerm pair when pair.Items.Count > 0 && pair.Items[0] is Atom
                {
                    Name:
                    RuntimeEtsCoreNames407.Heir
                }:
                    throw new NotSupportedException(RuntimeEtsCoreNames407.UnsupportedPolicy);
                default: throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            }
            rest = cell.Tail;
        }
        if (rest is not Nil) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        cancellation.ThrowIfCancellationRequested();
        if (kind == EtsCoreTableKind407.OrderedSet && writeConcurrency == EtsWriteConcurrency411.FixedHashLocks)
            throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);

        return new RuntimeEtsCoreOptions407(
            kind,
            protection,
            named,
            keyPosition
        )
        {
            Compressed = compressed,
            RequestedReadConcurrency = readConcurrency,
            RequestedWriteConcurrency = writeConcurrency,
            RequestedFixedWriteLocks = fixedWriteLocks,
            RequestedDecentralizedCounters = decentralizedCounters
        };
    }
}
