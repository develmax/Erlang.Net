namespace Erlang;

internal sealed class OtpMapRuntimeIdentityProvider(
    ProcessRuntime runtime,
    LocalNodeSnapshot local,
    RuntimeMapHashDomain domain,
    Action<MapTerm, OtpMapHashState64, Func<Term, ulong>> appendMap
) : OtpMapHashIdentityProvider, IDisposable
{
    private readonly object scopeGate = new();
    private NodeComparisonContext snapshots = Seed(runtime, local);

    private static NodeComparisonContext Seed(ProcessRuntime owner, LocalNodeSnapshot snapshot)
    {
        NodeComparisonContext context = default;
        context.Read(owner.LocalIdentity, snapshot);

        return context;
    }

    private readonly Dictionary<LocalNodeSnapshot, RuntimeMapHashNodeLease> nodeLeases = new();
    private bool closed;
    private readonly HashSet<Term> plannedTerms = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Term, ulong> keyHashes = new(ReferenceEqualityComparer.Instance);

    internal ulong Hash(Term value)
    {
        lock (scopeGate)
        {
            RequireOpen();
            try
            {
                PrepareMapKeys(value);

                return OtpMapInternalHash64.Hash(value, this);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
    }

    internal override bool TryImmediateWord(Term term, out ulong word)
    {
        RequireOpen();
        switch (term)
        {
            case Atom atom:
                word = ((ulong)runtime.ReadMapHashIdentity(() => AtomNameRegistry.IndexOf(atom.Name)) << RuntimeMapHashNames.AtomShift) | RuntimeMapHashNames.AtomTag;

                return true;
            case Pid pid when IsLocal(Snapshot(pid)):
                uint number = (uint)pid.Id;
                uint serial = (uint)(pid.Id >> RuntimeMapHashNames.WirePidSerialShift);
                // Wider public hosting IDs project into the native payload.
                // Hash collisions are allowed; native-valid IDs are unchanged.
                word = (((ulong)serial << RuntimeMapHashNames.PidNumberBits) | (number & RuntimeMapHashNames.PidNumberMask)) << RuntimeMapHashNames.ImmediateShift
                    | RuntimeMapHashNames.PidTag;

                return true;
            case PortTerm port when IsLocal(Snapshot(port)):
                word = ((port.Id & RuntimeMapHashNames.LocalPayloadMask) << RuntimeMapHashNames.ImmediateShift) | RuntimeMapHashNames.PortTag;

                return true;
            default:
                word = 0;

                return false;
        }
    }

    internal override void AppendStructured(Term term, OtpMapHashState64 state)
    {
        RequireOpen();
        switch (term)
        {
            case MapTerm map:
                appendMap(map, state, KnownKeyHash);

                return;
            case NativeRecordTerm record:
                AppendRecord(record, state);

                return;
            case FunctionTerm { ExternalIdentity: { } external }:
                state.Alpha(OtpMapHashNames.ExternalFun);
                state.Beta(runtime.ReadMapHashIdentity(() => domain.ExportWord(runtime, external)));

                return;
            case FunctionTerm { LocalIdentity: { } identity } function:
                // Native free-count storage is U8; larger own hosting/compiler
                // environments retain their actual count without truncation.
                var canonical = runtime.ReadMapHashIdentity(() => runtime.Functions.GetCanonicalIdentityForHash(identity, function.Arity));
                state.AlphaPair(OtpMapHashNames.LocalFun, (uint)function.Environment.Count);
                state.BetaPair(unchecked((uint)canonical.Index), unchecked((uint)canonical.OldUnique));
                state.Alpha(OtpMapHashNames.Immediate);
                state.Beta(((ulong)runtime.ReadMapHashIdentity(() => AtomNameRegistry.IndexOf(identity.Module.Name)) << RuntimeMapHashNames.AtomShift) | RuntimeMapHashNames.AtomTag);
                AppendLast(function.Environment, state);

                return;
            case ReferenceTerm reference:
                AppendReference(reference, state);

                return;
            case Pid pid:
                state.Alpha(OtpMapHashNames.ExternalPid);
                state.Beta(NodeWord(pid, Snapshot(pid)));
                state.AlphaPair((uint)pid.Id, (uint)(pid.Id >> RuntimeMapHashNames.WirePidSerialShift));

                return;
            case PortTerm port:
                state.Alpha(OtpMapHashNames.ExternalPort);
                state.Beta(NodeWord(port, Snapshot(port)));
                state.Alpha(port.Id);

                return;
            case FunctionTerm host:
                // Hosting extension: a genuine own registration, not an invented
                // ErlFunThing module, index, fingerprint or native address.
                var entry = runtime.ReadMapHashIdentity(() => domain.HostEntry(host));
                state.Alpha(RuntimeMapHashNames.HostFunctionExtension);
                state.Beta(entry.Word);
                state.Alpha((ulong)entry.Arity);

                return;
            default:
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private void AppendReference(ReferenceTerm reference, OtpMapHashState64 state)
    {
        var snapshot = Snapshot(reference);
        var numbers = reference.Words;
        if (IsLocal(snapshot))
        {
            uint first = numbers[0] & ReferenceIdentityLayout.LocalFirstWordMask;
            uint second = numbers.Count > 1 ? numbers[1] : 0;
            uint third = numbers.Count > 2 ? numbers[2] : 0;
            uint ownerLow = numbers.Count > ReferenceIdentityLayout.PidOwnerLowWord
                ? numbers[ReferenceIdentityLayout.PidOwnerLowWord] : 0;
            uint ownerHigh = numbers.Count > ReferenceIdentityLayout.PidOwnerHighWord
                ? numbers[ReferenceIdentityLayout.PidOwnerHighWord] : 0;
            // Native pid-refs have five words and a valid tagged owner. Public
            // hosting refs may be wider or zero-padded aliases. Mix their actual
            // nonzero owner pair; an all-zero extension keeps ordinary-ref hash.
            bool pidReference = (second & ReferenceIdentityLayout.PidMarker) != 0
                && (ownerLow != 0 || ownerHigh != 0);
            state.AlphaPair(OtpMapHashNames.LocalReference, first);
            state.BetaPair(second, third);
            if (pidReference)
                state.AlphaPair(ownerLow, ownerHigh);

            return;
        }
        state.AlphaPair(OtpMapHashNames.ExternalReference, (uint)numbers.Count);
        int index = 0;
        for (; index + 1 < numbers.Count; index += 2)
            state.BetaPair(numbers[index], numbers[index + 1]);
        if (index < numbers.Count)
            state.Beta(numbers[index]);
        state.Alpha(NodeWord(reference, snapshot));
    }

    private static void AppendRecord(NativeRecordTerm record, OtpMapHashState64 state)
    {
        state.Alpha(OtpMapHashNames.Record);
        state.Beta((ulong)record.Values.Count + 1);
        // The pinned native definition itself deliberately uses make_hash2.
        state.Push(new Integer(NativeRecordHash.Definition(record)));
        AppendLast(NativeRecordHash.Values(record), state);
    }

    private static void AppendLast(IReadOnlyList<Term> values, OtpMapHashState64 state)
    {
        for (int index = 0; index + 1 < values.Count; index++)
            state.Push(values[index]);
        if (values.Count != 0)
            state.Defer(values[^1]);
    }

    private void PrepareMapKeys(Term root)
    {
        var pending = new Stack<(Term Term, bool Expanded)>();
        pending.Push((root, false));
        while (pending.TryPop(out var work))
        {
            if (work.Expanded)
            {
                foreach (var pair in ((MapTerm)work.Term).Entries)
                    if (!keyHashes.ContainsKey(pair.Key))
                        keyHashes.Add(pair.Key, OtpMapInternalHash64.Hash(pair.Key, this));
                continue;
            }
            if (!plannedTerms.Add(work.Term))
                continue;
            if (work.Term is MapTerm map)
                pending.Push((map, true));
            foreach (Term child in RuntimeMapHashTraversal.Children(work.Term))
                pending.Push((child, false));
        }
    }

    private ulong KnownKeyHash(Term key) => keyHashes.TryGetValue(key, out ulong value)
        ? value : throw new InvalidOperationException(RuntimeMapHashDiagnostics.UnplannedKey);

    private ulong NodeWord(Term term, LocalNodeSnapshot snapshot)
    {
        return runtime.ReadMapHashIdentity(() =>
        {
            runtime.KnownNodes.RetainMapHashIdentifierIdentity(term, snapshot);
            if (!nodeLeases.TryGetValue(snapshot, out var lease))
            {
                lease = runtime.KnownNodes.AcquireMapHashNodeWord(term, snapshot);
                nodeLeases.Add(snapshot, lease);
            }
            else
                runtime.KnownNodes.ObserveMapHashEpochTerm(term, snapshot);

            return lease.Word;
        });
    }

    private void RequireOpen()
    {
        ObjectDisposedException.ThrowIf(closed, this);
        runtime.RequireMapHashOpen();
    }

    internal HamtHashLeases429 TransferHamtLeases429(HamtIndexOwner429 owner)
    {
        lock (scopeGate)
        {
            RequireOpen();
            var row = new HamtHashLeaseRow429(nodeLeases.Values.ToArray(), owner);
            var transferred = new HamtHashLeases429(row);
            owner.RegisterLeases(row);
            closed = true;
            nodeLeases.Clear();
            keyHashes.Clear();
            plannedTerms.Clear();

            return transferred;
        }
    }

    public void Dispose()
    {
        lock (scopeGate)
            DisposeCore();
    }

    private void DisposeCore()
    {
        if (closed)
            return;
        closed = true;
        List<Exception>? failures = null;
        foreach (var lease in nodeLeases.Values)
        {
            try
            {
                lease.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        nodeLeases.Clear();
        keyHashes.Clear();
        plannedTerms.Clear();
        if (failures is not null)
            throw new AggregateException(failures);
    }

    private bool IsLocal(LocalNodeSnapshot snapshot) => snapshot.Node == local.Node && snapshot.Creation == local.Creation;

    private LocalNodeSnapshot Snapshot(Pid term) => snapshots.Read(term.LocalIdentity, term.WireIdentity);

    private LocalNodeSnapshot Snapshot(PortTerm term) => snapshots.Read(term.LocalIdentity, term.WireIdentity);

    private LocalNodeSnapshot Snapshot(ReferenceTerm term) => snapshots.Read(term.LocalIdentity, term.WireIdentity);
}
