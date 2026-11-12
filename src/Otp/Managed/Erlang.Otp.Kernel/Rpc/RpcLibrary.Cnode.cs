// Derived from OTP-29.1.1 rpc.erl; Apache-2.0, Ericsson AB.
namespace Erlang.Otp;

public static partial class RpcLibrary
{
    private static async ValueTask<Term> CnodeLoop(ProcessContext context, Term state)
    {
        while (true)
        {
            Term message = (await context.ReceiveErlangAsync<Term>(value => value, Term.A(RpcProtocolNames.Infinity)))!;
            switch (message)
            {
                case TupleTerm { Items: [Atom { Name: RpcProtocolNames.IoRequest }, var from, var replyAs, var request] }:
                    TupleTerm response = (TupleTerm)await SourceRun(
                        context,
                        RpcFunctionNames.CnodeLoop,
                        1,
                        RpcSourceLines.CnodeRequest,
                        async () => await CnodeRequest(context, request, state)
                    );
                    state = response.Items[2];
                    await Bif(
                        context,
                        RpcBifNames.Send,
                        from,
                        Term.Tuple(Term.A(RpcProtocolNames.IoReply), replyAs, response.Items[1])
                    );
                    break;
                case TupleTerm { Items: [Atom { Name: RpcProtocolNames.Stop }, var requester, var reference, var to, var result] }:
                    await Reply(context, to, result);
                    await Bif(
                        context,
                        RpcBifNames.Send,
                        requester,
                        reference
                    );

                    return Term.A(RpcProtocolNames.Ok);
            }
        }
    }

    private static async ValueTask<TupleTerm> CnodeRequest(ProcessContext context, Term request, Term state)
    {
        if (request is TupleTerm { Items: [Atom { Name: RpcProtocolNames.PutChars }, var encoding, var characters] })
        {
            if (state is not TupleTerm { Items: [Atom { Name: RpcProtocolNames.CnodeCallGroupLeaderState }, var caller] })
            {
                throw SourceFailure(
                    context,
                    RpcFunctionNames.CnodePutChars,
                    3,
                    RpcSourceLines.CnodeRecord,
                    Term.Tuple(Term.A(RpcProtocolNames.Badrecord), state)
                );
            }
            Term converted = await SourceRun(
                context,
                RpcFunctionNames.CnodePutChars,
                3,
                RpcSourceLines.CnodeConvert,
                () => Call(
                    context,
                    RpcDependencyModules.Unicode,
                    RpcDependencyFunctions.CharactersToBinary,
                    characters,
                    encoding,
                    Term.A(RpcProtocolNames.Utf8)
                )
            );
            if (converted is BitString { IsBinary: true })
            {
                await Bif(
                    context,
                    RpcBifNames.Send,
                    caller,
                    Term.Tuple(Term.A(RpcProtocolNames.RexStdout), converted)
                );

                return Term.Tuple(Term.A(RpcProtocolNames.Ok), Term.A(RpcProtocolNames.Ok), state);
            }

            // Pinned upstream returns the atom state here, rather than its record.
            return Term.Tuple(Term.A(RpcProtocolNames.Error), Term.Tuple(Term.A(RpcProtocolNames.Error), converted), Term.A(RpcProtocolNames.State));
        }
        if (request is TupleTerm { Items: [Atom { Name: RpcProtocolNames.PutChars }, var enc, var module, var method, var args] })
        {
            try
            {
                Term text = await Bif(
                    context,
                    RpcBifNames.Apply,
                    module,
                    method,
                    args
                );

                return await CnodeRequest(context, Term.Tuple(Term.A(RpcProtocolNames.PutChars), enc, text), state);
            }
            catch (ErlangException)
            {
                return Term.Tuple(Term.A(RpcProtocolNames.Error), Term.Tuple(Term.A(RpcProtocolNames.Error), method), state);
            }
        }
        if (request is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Requests }, var requests] })
        {
            TupleTerm result = Term.Tuple(Term.A(RpcProtocolNames.Ok), Term.A(RpcProtocolNames.Ok), state);
            while (requests is Cons cell)
            {
                if (result.Items[0] is not Atom { Name: RpcProtocolNames.Ok })
                {
                    return result;
                }
                result = (TupleTerm)await SourceRun(
                    context,
                    RpcFunctionNames.CnodeCallGroupLeaderMultiRequest,
                    2,
                    RpcSourceLines.CnodeRequestItem,
                    async () => await CnodeRequest(context, cell.Head, result.Items[2])
                );
                requests = cell.Tail;
            }
            if (requests is not Nil)
            {
                throw Clause(
                    context,
                    RpcFunctionNames.CnodeCallGroupLeaderMultiRequest,
                    [requests, result],
                    RpcSourceLines.CnodeRequestsClause
                );
            }

            return result;
        }
        bool unsupported = request is Atom { Name: RpcProtocolNames.Getopts } || request is TupleTerm tuple && tuple.Items.Count > 0
            && (tuple.Items[0], tuple.Items.Count) is
                (Atom { Name: RpcProtocolNames.GetUntil }, 6) or (Atom { Name: RpcProtocolNames.GetChars }, 4) or (Atom { Name: RpcProtocolNames.GetLine }, 3)
                or (Atom { Name: RpcProtocolNames.GetGeometry or RpcProtocolNames.Setopts }, 2);

        return Term.Tuple(
            Term.A(RpcProtocolNames.Error),
            Term.Tuple(Term.A(RpcProtocolNames.Error), Term.A(unsupported ? RpcProtocolNames.Enotsup : RpcProtocolNames.Request)),
            state
        );
    }
}
