// Derived from OTP 29.1.1 erpc.erl, Apache-2.0. Copyright Ericsson AB 2020-2025.
namespace Erlang.Otp;

internal static partial class Erpc
{
    private static async ValueTask<Term> Multicall(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        try
        {
            ValidateTarget(
                ErpcAtoms.Module,
                arguments[1],
                arguments[2],
                arguments[3]
            );
            Term option = arguments.Count == 5 ? arguments[4] : ErpcAtoms.Infinity;
            Term timeout = option;
            bool alwaysSpawn = false;
            if (option is MapTerm options)
            {
                timeout = options.TryGet(ErpcAtoms.TimeoutKey, out var configured) ? configured! : ErpcAtoms.Infinity;
                alwaysSpawn = options.TryGet(ErpcAtoms.AlwaysSpawn, out var spawn) && DeleteOption(spawn!);
            }
            timeout = await ErpcTimeout.Normalize(context, timeout);
            Term deadline = timeout.Equals(ErpcAtoms.Infinity) ? timeout : await NativeDeadline(context, timeout);
            ReferenceTerm tag = context.Runtime.MakeReference();
            var requests = new List<Term>();
            var replies = new Dictionary<Term, Term>();
            bool local = false;
            bool badarg = false;
            Term nodes = arguments[0];
            while (nodes is Cons cell)
            {
                if (!alwaysSpawn && !local && deadline.Equals(ErpcAtoms.Infinity) && cell.Head.Equals(context.Runtime.NodeAtom))
                {
                    requests.Add(Term.A(ErpcNames.LocalCall));
                    local = true;
                }
                else
                {
                    try
                    {
                        ValidateTarget(
                            cell.Head,
                            arguments[1],
                            arguments[2],
                            arguments[3]
                        );
                        Term request = await SpawnRequest(
                            context,
                            cell.Head,
                            ErpcAtoms.Module,
                            ErpcAtoms.Execute,
                            Term.List(
                                tag,
                                arguments[1],
                                arguments[2],
                                arguments[3]
                            ),
                            Term.List(
                                Term.Tuple(Term.A(ProcessSpawnRequestNames.Reply), Term.A(ProcessSpawnRequestNames.ErrorOnly)),
                                Term.Tuple(Term.A(ErpcNames.ReplyTag), tag),
                                Term.Tuple(Term.A(ProcessSpawnOptionNames.Monitor), Term.List(Term.Tuple(Term.A(ErpcNames.Tag), tag)))
                            )
                        );
                        requests.Add(request);
                    }
                    catch (ErlangException)
                    {
                        badarg = true;
                        break;
                    }
                }
                nodes = cell.Tail;
            }
            badarg |= nodes is not Nil;
            if (local && !badarg)
            {
                try
                {
                    Term value = await Call(context, [context.Runtime.NodeAtom, arguments[1], arguments[2], arguments[3]]);
                    replies[Term.A(ErpcNames.LocalCall)] = Term.Tuple(Term.A(ErpcNames.Ok), value);
                }
                catch (ErlangException error)
                {
                    replies[Term.A(ErpcNames.LocalCall)] = Term.Tuple(Term.A(error.ExceptionClass), error.Reason);
                }
            }
            var pending = requests.OfType<ReferenceTerm>().ToHashSet();
            if (badarg)
            {
                foreach (ReferenceTerm request in pending.ToArray())
                {
                    if (await Abandon(context, request))
                        pending.Remove(request);
                }
            }
            bool expired = badarg;
            while (pending.Count > 0)
            {
                Term remaining = expired ? Term.I(0) : await TimeLeft(context, deadline);
                TupleTerm? reply = await context.ReceiveErlangAsync(value => SelectTagged(value, tag), remaining);
                if (reply is not null)
                {
                    Term request = reply.Items[1];
                    try
                    {
                        Term answer = await ResultByType(
                            context,
                            Term.A(reply.Items.Count == 4 ? ProcessSpawnRequestNames.DefaultTag : ErpcNames.Down),
                            request,
                            tag,
                            reply.Items[^1]
                        );
                        replies[request] = Term.Tuple(Term.A(ErpcNames.Ok), answer);
                    }
                    catch (ErlangException error)
                    {
                        replies[request] = Term.Tuple(Term.A(error.ExceptionClass), error.Reason);
                    }
                    if (request is ReferenceTerm reference)
                        pending.Remove(reference);
                }
                else if (!expired)
                {
                    expired = true;
                    foreach (ReferenceTerm request in pending.ToArray())
                    {
                        if (await Abandon(context, request))
                            pending.Remove(request);
                    }
                }
                else
                {
                    // Abandon leaves only already queued replies. This mirrors the
                    // upstream final zero-timeout drain after all monitors retire.
                    if (badarg)
                    {
                        foreach (ReferenceTerm request in pending.ToArray())
                        {
                            if (await Abandon(context, request))
                                pending.Remove(request);
                        }
                    }
                    break;
                }
            }
            if (badarg)
                throw Failure(ErpcAtoms.BadArgument);

            return Cons.From(requests.Select(request => replies.TryGetValue(request, out var value) ? value : Term.Tuple(ErpcAtoms.Error, Term.Tuple(ErpcAtoms.Module, ErpcAtoms.Timeout))));
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && !error.Reason.Equals(Term.A(ErpcNames.InternalError)))
        {
            throw Failure(ErpcAtoms.BadArgument);
        }
    }

    private static TupleTerm? SelectTagged(Term value, ReferenceTerm tag)
    {
        if (value is TupleTerm { Items.Count: 4 } spawn && spawn.Items[0].Equals(tag) && spawn.Items[2].Equals(ErpcAtoms.Error))
            return spawn;
        if (value is TupleTerm { Items.Count: 5 } down && down.Items[0].Equals(tag) && down.Items[2] is Atom { Name: ProcessMonitorKinds.Process })
            return down;

        return null;
    }

    private static ValueTask<Term> Clock(ProcessContext context, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ErpcNames.MonotonicTime,
            arguments
        );

    private static ValueTask<Term> ConvertTime(
        ProcessContext context,
        Term value,
        string from,
        string to
    )
        => context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ErpcNames.ConvertTimeUnit,
            value,
            Term.A(from),
            Term.A(to)
        );

    private static async ValueTask<Term> NativeDeadline(ProcessContext context, Term timeout)
    {
        if (timeout is Integer milliseconds && milliseconds.Value == ErpcNames.MaximumTimeout)
        {
            var now = (Integer)await Clock(context, Term.A(ErpcNames.Millisecond));

            return await ConvertTime(
                context,
                new Integer(now.Value + milliseconds.Value),
                ErpcNames.Millisecond,
                ErpcNames.Native
            );
        }
        var current = (Integer)await Clock(context);
        var delta = (Integer)await ConvertTime(
            context,
            timeout,
            ErpcNames.Millisecond,
            ErpcNames.Native
        );

        return new Integer(current.Value + delta.Value);
    }

    private static async ValueTask<Term> TimeLeft(ProcessContext context, Term deadline)
    {
        if (deadline.Equals(ErpcAtoms.Infinity))
            return deadline;
        var current = (Integer)await Clock(context);
        var remaining = ((Integer)deadline).Value - current.Value;
        if (remaining <= 0)
            return Term.I(0);
        var converted = (Integer)await ConvertTime(
            context,
            new Integer(remaining - 1),
            ErpcNames.Native,
            ErpcNames.Millisecond
        );

        return new Integer(converted.Value + 1);
    }
}
