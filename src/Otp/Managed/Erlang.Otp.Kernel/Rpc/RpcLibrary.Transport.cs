// Derived from OTP-29.1.1 rpc.erl; Apache-2.0, Ericsson AB.
namespace Erlang.Otp;

public static partial class RpcLibrary
{
    private static async ValueTask<Term> ServerRequest(
        ProcessContext context,
        Term node,
        Term request,
        Term timeout
    )
    {
        if (timeout is Atom { Name: RpcProtocolNames.Infinity })
        {
            return RpcCheck(
                await Catch(() => Call(
                    context,
                    RpcDependencyModules.GenServer,
                    RpcDependencyFunctions.Call,
                    Term.Tuple(Term.A(RpcProtocolNames.Rex), node),
                    request,
                    timeout
                )),
                false
            );
        }
        Term tag = await Bif(context, RpcBifNames.MakeRef);
        var spawned = (TupleTerm)await Spawn(
            context,
            RpcProtocolNames.SpawnMonitor,
            RpcWorkerKind.Middleman,
            [node, request, timeout, tag]
        );
        Term receiver = spawned.Items[0], reference = spawned.Items[1];
        var response = await context.ReceiveErlangAsync<Term>(
            value => value is TupleTerm
            {
                Items: [Atom { Name: RpcProtocolNames.DOWN }, var monitor, _, _, _]
            } && monitor.Equals(reference) ? value : null,
            Term.A(RpcProtocolNames.Infinity)
        );
        Term reason = ((TupleTerm)response!).Items[4];
        if (reason is TupleTerm { Items: [var pid, var correlation, var result] } && pid.Equals(receiver) && correlation.Equals(tag))
        {
            return RpcCheck(result, false);
        }

        return RpcCheck(Term.Tuple(Term.A(RpcProtocolNames.ExitTag), reason), true);
    }

    private static Term RpcCheck(Term value, bool timed)
    {
        if (value is TupleTerm { Items: [Atom { Name: RpcProtocolNames.ExitTag }, var reason] })
        {
            if (timed && reason is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Timeout }, _] })
            {
                return Term.Tuple(Term.A(RpcProtocolNames.Badrpc), Term.A(RpcProtocolNames.Timeout));
            }
            if (timed && reason is TupleTerm { Items: [Atom { Name: RpcProtocolNames.TimeoutValue }, _] })
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            if (reason is TupleTerm { Items: [TupleTerm { Items: [Atom { Name: RpcProtocolNames.Nodedown }, _] }, _] })
            {
                return Term.Tuple(Term.A(RpcProtocolNames.Badrpc), Term.A(RpcProtocolNames.Nodedown));
            }

            return Term.Tuple(Term.A(RpcProtocolNames.Badrpc), value);
        }

        return value;
    }

    private static async ValueTask<Term> Catch(Func<ValueTask<Term>> body)
    {
        try
        {
            return await body();
        }
        catch (ErlangException error)
        {
            return error.ExceptionClass switch
            {
                ErlangExceptionClasses.Throw => error.Reason,
                ErlangExceptionClasses.Exit => Term.Tuple(Term.A(RpcProtocolNames.ExitTag), error.Reason),
                _ => Term.Tuple(Term.A(RpcProtocolNames.ExitTag), Term.Tuple(error.Reason, error.StackTraceTerm))
            };
        }
    }

    private static async ValueTask<Term> StartMonitor(ProcessContext context, Term node, Term name)
    {
        Term destination = Term.Tuple(name, node);
        Term reference;
        if (context.Runtime.NodeAtom is Atom { Name: RpcProtocolNames.NonodeNohost } && !node.Equals(context.Runtime.NodeAtom))
        {
            reference = await Bif(context, RpcBifNames.MakeRef);
            await Bif(
                context,
                RpcBifNames.Send,
                context.Self,
                Term.Tuple(
                    Term.A(RpcProtocolNames.DOWN),
                    reference,
                    Term.A(RpcProtocolNames.Process),
                    destination,
                    Term.A(RpcProtocolNames.Noconnection)
                )
            );
        }
        else
        {
            reference = await Bif(
                context,
                RpcBifNames.Monitor,
                Term.A(RpcProtocolNames.Process),
                destination
            );
        }

        return Term.Tuple(node, reference);
    }

    private static ValueTask<Term> Demonitor(ProcessContext context, Term reference)
        => Bif(
            context,
            RpcBifNames.Demonitor,
            reference,
            Term.List(Term.A(RpcProtocolNames.Flush))
        );

    private static async ValueTask<Term> ServerCall(
        ProcessContext context,
        Term node,
        Term name,
        Term wrapper,
        Term message
    )
    {
        if (context.Runtime.NodeAtom is Atom { Name: RpcProtocolNames.NonodeNohost } && !node.Equals(context.Runtime.NodeAtom))
        {
            return Term.Tuple(Term.A(RpcProtocolNames.Error), Term.A(RpcProtocolNames.Nodedown));
        }
        Term reference = await Bif(
            context,
            RpcBifNames.Monitor,
            Term.A(RpcProtocolNames.Process),
            Term.Tuple(name, node)
        );
        await Bif(
            context,
            RpcBifNames.Send,
            Term.Tuple(name, node),
            Term.Tuple(context.Self, message)
        );
        var response = (TupleTerm)(await context.ReceiveErlangAsync<Term>(
            value => value switch
        {
            TupleTerm { Items: [Atom { Name: RpcProtocolNames.DOWN }, var monitor, _, _, _] } when monitor.Equals(reference) => value,
            TupleTerm { Items: [var tag, var from, _] } when tag.Equals(wrapper) && from.Equals(node) => value,
            _ => null
        },
            Term.A(RpcProtocolNames.Infinity)
        ))!;
        if (response.Items.Count == 5)
        {
            return Term.Tuple(Term.A(RpcProtocolNames.Error), Term.A(RpcProtocolNames.Nodedown));
        }
        await Demonitor(context, reference);

        return response.Items[2];
    }

    private static async ValueTask<Term> Broadcast(
        ProcessContext context,
        Term nodes,
        Term name,
        Term message
    )
    {
        var monitors = new List<TupleTerm>();
        while (nodes is Cons cell)
        {
            if (cell.Head is Atom)
            {
                monitors.Insert(0, (TupleTerm)await StartMonitor(context, cell.Head, name));
                await Catch(() => Bif(
                    context,
                    RpcBifNames.Send,
                    Term.Tuple(name, cell.Head),
                    Term.Tuple(context.Self, message)
                ));
            }
            nodes = cell.Tail;
        }
        if (nodes is not Nil)
        {
            throw Clause(
                context,
                RpcFunctionNames.SendNodes,
                [nodes, name, message, Cons.From(monitors)],
                RpcSourceLines.SendNodesClause
            );
        }
        var replies = new List<Term>();
        var bad = new List<Term>();
        foreach (TupleTerm monitor in monitors)
        {
            Term node = monitor.Items[0], reference = monitor.Items[1];
            var response = (TupleTerm)(await context.ReceiveErlangAsync<Term>(
                value => value switch
            {
                TupleTerm { Items: [Atom { Name: RpcProtocolNames.DOWN }, var id, _, _, _] } when id.Equals(reference) => value,
                TupleTerm { Items: [Atom { Name: RpcProtocolNames.Rex }, var from, TupleTerm { Items: [Atom { Name: RpcProtocolNames.NonexistingName }, _] }] }
                    when from.Equals(node) => value,
                TupleTerm { Items: [var tag, var from, _] } when tag.Equals(name) && from.Equals(node) => value,
                _ => null
            },
                Term.A(RpcProtocolNames.Infinity)
            ))!;
            if (response.Items.Count == 5)
            {
                bad.Insert(0, node);
            }
            else
            {
                await Demonitor(context, reference);
                if (response.Items[0] is Atom { Name: RpcProtocolNames.Rex } && response.Items[2] is TupleTerm { Items: [Atom { Name: RpcProtocolNames.NonexistingName }, _] })
                {
                    bad.Insert(0, node);
                }
                else
                {
                    replies.Insert(0, response.Items[2]);
                }
            }
        }

        return Term.Tuple(Cons.From(replies), Cons.From(bad));
    }
}
