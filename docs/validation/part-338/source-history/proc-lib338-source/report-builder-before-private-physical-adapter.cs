namespace Erlang.Otp;

internal sealed class ProcLibCrashReportBuilder(
    ProcessContext context,
    Func<ValueTask<Term>> getFormatDepth,
    Func<Term, ValueTask<Term>> limitTerm
)
{
    public async ValueTask<Term> Build(
        string exceptionClass,
        Term reason,
        Term initialCall,
        Term stackTrace
    )
    {
        Term own = await OwnReport(
            exceptionClass,
            reason,
            initialCall,
            stackTrace
        );
        var (_, neighbours) = await Visit(await Adjacent(context.Self), ProcLibCrashReportNames.MaximumNeighbours, [context.Self]);
        var linked = new List<Term>();
        foreach (Term pid in neighbours.Where(pid => !pid.Equals(context.Self)))
        {
            await context.ReduceAsync();
            Term report = await NeighbourReport(pid);
            if (!report.Equals(ProcessInfoAtoms.Undefined))
                linked.Add(Term.Tuple(Term.A(ProcLibCrashReportNames.Neighbour), report));
        }

        return Term.List(own, Cons.From(linked));
    }

    private async ValueTask<Term> OwnReport(
        string exceptionClass,
        Term reason,
        Term initialCall,
        Term stackTrace
    )
    {
        Term information = await Information(context.Self, ProcLibCrashReportAtoms.OwnKeys);
        Term dictionary = Find(ProcLibCrashReportAtoms.Dictionary, information);
        if (dictionary is not TupleTerm { Items: [_, var entries] })
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), dictionary));
        Term ancestors = context.Dictionary.GetValueOrDefault(ProcLibAtoms.Ancestors, Nil.Value);
        if (ancestors is not Cons and not Nil)
            ancestors = Nil.Value;
        Term[] body = [
            Pair(ProcLibCrashReportNames.Pid, context.Self),
            Find(ProcLibCrashReportNames.RegisteredName, information),
            Pair(
                ProcLibCrashReportNames.ProcessLabel,
                context.Dictionary.GetValueOrDefault(ProcLibIntrospectionAtoms.ProcessLabel, ProcessInfoAtoms.Undefined)
            ),
            Pair(ProcLibCrashReportNames.ErrorInfo, Term.Tuple(Term.A(exceptionClass), reason, stackTrace)),
            Pair(ProcLibCrashReportNames.Ancestors, ancestors),
            Find(ProcLibCrashReportNames.MessageQueueLength, information),
            Pair(ProcLibCrashReportNames.Messages, await limitTerm(await Messages())),
            Find(ProcLibCrashReportNames.Links, information),
            Pair(ProcLibCrashReportNames.Dictionary, await limitTerm(CleanDictionary(entries))),
            Find(ProcLibCrashReportNames.TrapExit, information),
            Find(ProcLibCrashReportNames.Status, information),
            Find(ProcLibCrashReportNames.HeapSize, information),
            Find(ProcLibCrashReportNames.StackSize, information),
            Find(ProcLibCrashReportNames.Reductions, information)
        ];
        Term result = Cons.From(body);

        return initialCall is Nil ? result : new Cons(Pair(ProcLibCrashReportNames.InitialCall, initialCall), result);
    }

    private async ValueTask<Term> Messages()
    {
        Term depth = await getFormatDepth();
        if (depth.Equals(ProcLibCrashReportAtoms.Unlimited))
        {
            Term information = await Information(context.Self, Term.A(ProcLibCrashReportNames.Messages));
            if (information is TupleTerm { Items: [_, var messages] })
                return messages;

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), information));
        }
        var result = new List<Term>();
        while (depth is not Integer integer || !integer.Value.IsZero)
        {
            Term? message = await context.ReceiveErlangAsync(value => value, Term.I(0));
            if (message is null)
                break;
            result.Add(message);
            if (depth is not Integer count)
                throw new ErlangException(ErlangErrorReasons.BadArithmetic);
            depth = new Integer(count.Value - 1);
        }

        return Cons.From(result);
    }

    private static Term CleanDictionary(Term dictionary)
    {
        var result = new List<Term>();
        foreach (Term entry in Cons.Items(dictionary))
        {
            if (entry is TupleTerm { Items: [var key, _] } &&
                (key.Equals(ProcLibAtoms.Ancestors) || key.Equals(ProcLibAtoms.InitialCall) || key.Equals(ProcLibIntrospectionAtoms.ProcessLabel)))
                continue;
            result.Add(entry);
        }

        return Cons.From(result);
    }

    private async ValueTask<Term> NeighbourReport(Term pid)
    {
        Term information = await Information(pid, ProcLibCrashReportAtoms.NeighbourKeys);
        Term dictionary = await Information(pid, ProcLibCrashReportAtoms.NeighbourDictionaryKeys);
        if (information.Equals(ProcessInfoAtoms.Undefined) || dictionary.Equals(ProcessInfoAtoms.Undefined))
            return ProcessInfoAtoms.Undefined;
        Term initial = DictionaryInformation(
            ProcLibAtoms.InitialCall,
            ProcLibCrashReportAtoms.InitialCall,
            dictionary,
            ProcessInfoAtoms.Undefined
        );
        initial = initial is TupleTerm { Items: [_, TupleTerm { Items: [var module, var function, var arity] }] }
            ? Pair(
                ProcLibCrashReportNames.InitialCall,
                Term.Tuple(module, function, await ProcLibIntrospection.MakeDummyArguments(context, arity))
            )
            : Find(ProcLibCrashReportNames.InitialCall, information);

        return Term.List(
            Pair(ProcLibCrashReportNames.Pid, pid),
            Find(ProcLibCrashReportNames.RegisteredName, information),
            DictionaryInformation(
                ProcLibIntrospectionAtoms.ProcessLabel,
                ProcLibCrashReportAtoms.ProcessLabel,
                dictionary,
                ProcessInfoAtoms.Undefined
            ),
            initial,
            Find(ProcLibCrashReportNames.CurrentFunction, information),
            DictionaryInformation(
                ProcLibAtoms.Ancestors,
                ProcLibCrashReportAtoms.Ancestors,
                dictionary,
                Nil.Value
            ),
            Find(ProcLibCrashReportNames.MessageQueueLength, information),
            Find(ProcLibCrashReportNames.Links, information),
            Find(ProcLibCrashReportNames.TrapExit, information),
            Find(ProcLibCrashReportNames.Status, information),
            Find(ProcLibCrashReportNames.HeapSize, information),
            Find(ProcLibCrashReportNames.StackSize, information),
            Find(ProcLibCrashReportNames.Reductions, information),
            Find(ProcLibCrashReportNames.CurrentStacktrace, information)
        );
    }

    private static Term DictionaryInformation(
        Term dictionaryKey,
        Term infoKey,
        Term information,
        Term fallback
    )
    {
        Term entry = Find(Term.Tuple(ProcLibCrashReportAtoms.Dictionary, dictionaryKey), information);
        if (entry.Equals(ProcLibCrashReportAtoms.False))
        {
            entry = Find(dictionaryKey, information);
            if (entry.Equals(ProcLibCrashReportAtoms.False))
                return Term.Tuple(infoKey, fallback);
            if (entry is TupleTerm { Items: [_, var oldValue] })
                return Term.Tuple(infoKey, oldValue);
        }
        else if (entry is TupleTerm { Items: [_, var value] })
        {
            return Term.Tuple(infoKey, value.Equals(ProcessInfoAtoms.Undefined) ? fallback : value);
        }

        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), entry));
    }

    private async ValueTask<(int Remaining, List<Term> Visited)> Visit(Term pending, int remaining, List<Term> visited)
    {
        if (remaining <= 0 || pending is not Cons current)
            return (remaining, visited);
        await context.ReduceAsync();
        if (visited.Any(value => value.Equals(current.Head)))
            return await Visit(current.Tail, remaining, visited);
        Term children = await Adjacent(current.Head);
        var next = new List<Term> { current.Head };
        next.AddRange(visited);
        var siblings = await Visit(current.Tail, remaining - 1, next);

        return await Visit(children, siblings.Remaining, siblings.Visited);
    }

    private async ValueTask<Term> Adjacent(Term pid)
    {
        Term information = await Information(pid, ProcLibCrashReportAtoms.Links);
        if (information is not TupleTerm { Items: [_, var links] })
            return Nil.Value;
        var result = new List<Term>();
        foreach (Term linked in Cons.Items(links))
        {
            Term trap = await Information(linked, ProcLibCrashReportAtoms.TrapExit);
            if (trap is TupleTerm { Items: [_, var flag] } && flag.Equals(ProcLibCrashReportAtoms.False))
                result.Add(linked);
        }

        return Cons.From(result);
    }

    private async ValueTask<Term> Information(Term pid, Term item)
    {
        try
        {
            Term node = await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ProcLibCrashReportNames.Node,
                pid
            );
            Term result;
            if (node.Equals(context.Runtime.NodeAtom))
            {
                result = await context.Runtime.Modules.Call(
                    context,
                    ProcLibNames.Erlang,
                    ProcLibCrashReportNames.ProcessInfo,
                    pid,
                    item
                );
            }
            else
            {
                Term nodes = await context.Runtime.Modules.Call(context, ProcLibNames.Erlang, ProcLibCrashReportNames.Nodes);
                if (!Cons.Items(nodes).Any(connected => connected.Equals(node)))
                    return Term.A(ProcLibCrashReportNames.Hidden);
                result = await context.Runtime.Modules.Call(
                    context,
                    ProcLibCrashReportNames.Rpc,
                    ProcLibCrashReportNames.Call,
                    node,
                    Term.A(ProcLibNames.Erlang),
                    Term.A(ProcLibCrashReportNames.ProcessInfo),
                    Term.List(pid, item)
                );
                if (result is TupleTerm { Items: [Atom { Name: ProcLibCrashReportNames.BadRpc }, var error] })
                    result = error is Atom { Name: ProcLibCrashReportNames.NodeDown } ? ProcessInfoAtoms.Undefined : error;
            }
            if (result is TupleTerm { Items: [var key, var value] } && key.Equals(ProcLibCrashReportAtoms.ProcessLabelQuery))
                return Term.Tuple(ProcLibCrashReportAtoms.ProcessLabel, value);

            return result;
        }
        catch (ErlangException)
        {
            return ProcessInfoAtoms.Undefined;
        }
    }

    private static Term Pair(string key, Term value) => Term.Tuple(Term.A(key), value);

    private static Term Find(string key, Term list) => Find(Term.A(key), list);

    private static Term Find(Term key, Term list)
    {
        while (list is Cons item)
        {
            if (item.Head is TupleTerm tuple && tuple.Items.Count > 0 && tuple.Items[0].Equals(key))
                return tuple;
            list = item.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return ProcLibCrashReportAtoms.False;
    }
}
