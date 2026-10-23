namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal bool Contains(Term value)
    {
        lock (gate)
        {
            if (value is Atom name) value = runtime.WhereIs(name.Name);

            return value is PortTerm identity && ports.TryGetValue(identity, out var row)
                && !row.Closing && !row.StopRequested && row.OpenResultKnown && row.OpenSucceeded;
        }
    }

    internal PortTerm[] Snapshot()
    {
        lock (gate)
            return ports.Values.Where(row => !row.Closing && !row.StopRequested && row.OpenResultKnown && row.OpenSucceeded)
                .Select(row => row.Identity).ToArray();
    }

    internal Term GetData(Term identity)
    {
        lock (gate) return Require(identity).Data;
    }

    internal Task AwaitCommands(Term identity, ProcessContext? caller = null)
    {
        if (caller is not null) runtime.RequireDriverParentActor422(caller);
        lock (gate)
        {
            var row = Require(identity);

            return caller is null ? row.Commands : EnqueueSignal454(
                row,
                caller.Self,
                false,
                () => Task.FromResult<Term>(Nil.Value)
            );
        }
    }

    internal Term SetData(Term identity, Term data)
    {
        lock (gate) Require(identity).Data = data;

        return Term.A(ErlangBooleanAtoms.True);
    }

    internal Term Info(Term identity, Term? item)
    {
        lock (gate)
        {
            var row = Require(identity);
            if (item is not null) return InfoItem(row, item);
            var result = new List<Term>();
            if (runtime.PortRegisteredName(row.Identity) is string name)
                result.Add(Term.Tuple(Term.A(PortNames.RegisteredName), Term.A(name)));
            foreach (string key in new[] { PortNames.Name, PortNames.Links, PortNames.Id,
                PortNames.Connected, PortNames.Input, PortNames.Output, PortNames.OsPid })
                result.Add(InfoItem(row, Term.A(key)));

            return Cons.From(result);
        }
    }

    internal ValueTask<Term> InfoQueued454(ProcessContext caller, Term identity, Term? item)
    {
        runtime.RequireDriverParentActor422(caller);
        lock (gate)
        {
            var row = Require(identity);
            Task<Term> operation = EnqueueSignal454(
                row,
                caller.Self,
                false,
                () =>
            {
                lock (gate)
                {
                    Term result = row.Closing || row.StopRequested || row.PhysicallyStopped
                        ? Term.A(ProcessRegistryAtoms.Undefined) : Info(row.Identity, item);

                    return Task.FromResult(result);
                }
            }
            );

            return new(operation.WaitAsync(caller.Cancellation));
        }
    }

    private Term InfoItem(NativeDriverPortRow422 row, Term item)
    {
        if (item is not Atom key) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (key.Name == PortNames.RegisteredName && runtime.PortRegisteredName(row.Identity) is null) return Nil.Value;
        Term value = key.Name switch
        {
            PortNames.Name => Cons.From(row.Command.Select(value => Term.I(value))),
            PortNames.Links => Cons.From(row.Links),
            PortNames.Id => new Integer(row.Identity.Id),
            PortNames.Connected => row.Connected,
            PortNames.Input => Term.I(row.InputBytes),
            PortNames.Output => Term.I(row.OutputBytes),
            PortNames.QueueSize => throw new NotSupportedException(NativeDriverPortNames422.QueueInfoUnsupported),
            PortNames.OsPid => Term.A(ProcessRegistryAtoms.Undefined),
            PortNames.RegisteredName => runtime.PortRegisteredName(row.Identity) is string name ? Term.A(name) : Nil.Value,
            PortNames.MonitoredBy => runtime.PortMonitoredBy(row.Identity),
            PortNames.Monitors => throw new NotSupportedException(NativeDriverPortNames422.MonitorInfoUnsupported),
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };

        return Term.Tuple(key, value);
    }
}
