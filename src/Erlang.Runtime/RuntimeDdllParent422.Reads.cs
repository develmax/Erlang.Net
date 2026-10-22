using System.Text;

namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    // Caller holds the shared runtime gate. This is a linked descriptor list,
    // not dictionary enumeration; native registry adds prepend and may duplicate.
    private DdllParentRow422? Find(ErlDriverName420 name)
        => drivers.FirstOrDefault(row => row.Name.Key == name.Key);

    private static Term NativeName(DdllParentRow422 row)
        => Term.A(Encoding.Latin1.GetString(row.Name.Bytes));

    private static Term SoftError(string reason)
        => Term.Tuple(Term.A(DdllParentAtoms422.Error), Term.A(reason));

    private static Term Ok(string status, ReferenceTerm? monitor = null)
        => monitor is null
            ? Term.Tuple(Term.A(DdllParentAtoms422.Ok), Term.A(status))
            : Term.Tuple(Term.A(DdllParentAtoms422.Ok), Term.A(status), monitor);

    private Term LoadedDriversCore()
    {
        Term result = Nil.Value;
        foreach (var row in drivers)
            result = new Cons(Cons.From(row.Name.Bytes.ToArray().Select(value => (Term)Term.I(value))), result);

        return Term.Tuple(Term.A(DdllParentAtoms422.Ok), result);
    }

    private Term InfoCore(ErlDriverName420 name, Term item)
    {
        DdllParentRow422 row = Find(name) ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (item is not Atom key) throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool permanent = row.State == DdllParentStateKind422.Permanent;
        switch (key.Name)
        {
            case DdllParentAtoms422.DriverOptions:
                return row.Linked ? Term.A(DdllParentAtoms422.Linked)
                    : row.KillPorts ? Term.List(Term.A(DdllParentAtoms422.KillPorts)) : Nil.Value;
            case DdllParentAtoms422.PortCount:
                return row.Linked ? Term.A(DdllParentAtoms422.Linked)
                    : permanent ? Term.A(DdllParentAtoms422.Permanent) : Term.I(row.Ports.Count);
            case DdllParentAtoms422.Linked:
                return CoreModules.Boolean(row.Linked);
            case DdllParentAtoms422.Permanent:
                return CoreModules.Boolean(permanent);
            case DdllParentAtoms422.Processes:
                return ProcessInfoCore(row, DdllParentReferenceKind422.Loaded);
            case DdllParentAtoms422.AwaitingLoad:
                return ProcessInfoCore(row, DdllParentReferenceKind422.AwaitingLoad);
            case DdllParentAtoms422.AwaitingUnload:
                return ProcessInfoCore(row, DdllParentReferenceKind422.AwaitingUnload);
            default:
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private static Term ProcessInfoCore(DdllParentRow422 row, DdllParentReferenceKind422 filter)
    {
        if (row.Linked) return Term.A(DdllParentAtoms422.Linked);
        if (row.State == DdllParentStateKind422.Permanent) return Term.A(DdllParentAtoms422.Permanent);
        var order = new List<Pid>();
        var counts = new Dictionary<Pid, int>();
        foreach (var reference in row.Processes)
        {
            var kind = reference.Kind == DdllParentReferenceKind422.AwaitingUnloadOnly
                ? DdllParentReferenceKind422.AwaitingUnload : reference.Kind;
            if (kind != filter) continue;
            if (counts.TryGetValue(reference.Owner, out int count)) counts[reference.Owner] = checked(count + 1);
            else
            {
                order.Add(reference.Owner);
                counts.Add(reference.Owner, 1);
            }
        }
        Term result = Nil.Value;
        foreach (Pid pid in order)
            result = new Cons(Term.Tuple(pid, Term.I(counts[pid])), result);

        return result;
    }

    private ReferenceTerm AddMonitor(DdllParentRow422 row, Pid caller, DdllParentReferenceKind422 kind)
    {
        ReferenceTerm reference = runtime.MakeReference();
        row.Processes.AddFirst(new DdllParentProcessReference422(caller, kind, reference));

        return reference;
    }

    private void Notify(
        Pid target,
        ReferenceTerm reference,
        Term name,
        string direction,
        Term tag
    )
        => runtime.SendSystem(target, Term.Tuple(
            Term.A(direction),
            reference,
            Term.A(DdllParentAtoms422.Driver),
            name,
            tag
        ));

    private void NotifyAll(
        DdllParentRow422 row,
        Func<DdllParentReferenceKind422, bool> selected,
        string direction,
        Term tag
    )
    {
        for (var node = row.Processes.First; node is not null;)
        {
            var next = node.Next;
            var value = node.Value;
            if (selected(value.Kind))
            {
                row.Processes.Remove(node);
                Notify(
                    value.Owner,
                    value.Monitor!,
                    NativeName(row),
                    direction,
                    tag
                );
            }
            node = next;
        }
    }

    private ReferenceTerm MonitorCore(
        Pid caller,
        Term type,
        Term description,
        CancellationToken cancellation
    )
    {
        if (type is not Atom { Name: DdllParentAtoms422.Driver }
            || description is not TupleTerm { Items.Count: 2 } tuple)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ErlDriverName420 name = ErlDriverName420.Parse(tuple.Items[0], cancellation);
        if (tuple.Items[1] is not Atom { Name: DdllParentAtoms422.Loaded or DdllParentAtoms422.Unloaded or DdllParentAtoms422.UnloadedOnly } state)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        DdllParentRow422? row = Find(name);
        string? direction = null, tag = null;
        if (row is null)
        {
            direction = DdllParentAtoms422.Down;
            tag = DdllParentAtoms422.Unloaded;
        }
        else if (row.Linked || row.State == DdllParentStateKind422.Permanent)
        {
            direction = DdllParentAtoms422.Up;
            tag = DdllParentAtoms422.Permanent;
        }
        else if (state.Name == DdllParentAtoms422.Loaded)
        {
            if (row.State == DdllParentStateKind422.Healthy)
            {
                direction = DdllParentAtoms422.Up;
                tag = DdllParentAtoms422.Loaded;
            }
            else if (row.State is DdllParentStateKind422.Unloading or DdllParentStateKind422.ForceUnloading)
            {
                direction = DdllParentAtoms422.Down;
                tag = DdllParentAtoms422.LoadCancelled;
            }
        }
        if (direction is not null)
        {
            ReferenceTerm immediate = runtime.MakeReference();
            Notify(
                caller,
                immediate,
                tuple.Items[0],
                direction,
                Term.A(tag!)
            );

            return immediate;
        }

        return AddMonitor(
            row!,
            caller,
            state.Name == DdllParentAtoms422.Loaded
            ? DdllParentReferenceKind422.AwaitingLoad : state.Name == DdllParentAtoms422.Unloaded
                ? DdllParentReferenceKind422.AwaitingUnload : DdllParentReferenceKind422.AwaitingUnloadOnly
        );
    }

    private Term DemonitorCore(Pid caller, Term term)
    {
        if (term is not ReferenceTerm reference || !runtime.DriverInternalReference422(reference))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        foreach (var row in drivers)
        {
            for (var node = row.Processes.First; node is not null; node = node.Next)
                if (node.Value.Kind != DdllParentReferenceKind422.Loaded
                    && node.Value.Owner.Equals(caller) && node.Value.Monitor!.Equals(reference))
                {
                    row.Processes.Remove(node);

                    return Term.A(ErlangBooleanAtoms.True);
                }
        }

        return Term.A(ErlangBooleanAtoms.True);
    }
}
