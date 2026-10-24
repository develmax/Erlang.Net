using Erlang.Otp;

namespace Erlang.Tests;

public static class ConnectedRpcLocalHostChecks
{
    public static async Task LocalCallerAndExceptions()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(OtpModules.Register);
        runtime.Modules.Register(
            ConnectedRpcHostNames.Module,
            ConnectedRpcHostNames.Error,
            0,
            (_, _) => throw new ErlangException(ErlangErrorReasons.SystemLimit)
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            async ValueTask<Term> Call(
                string rpcModule,
                string module,
                string function,
                Term arguments,
                Term? options = null
            )
                => await runtime.Modules.Call(
                    context,
                    rpcModule,
                    ConnectedRpcHostNames.Call,
                    options is null
                        ? [runtime.NodeAtom, Term.A(module), Term.A(function), arguments]
                        : [runtime.NodeAtom, Term.A(module), Term.A(function), arguments, options]
                );
            Require((await Call(
                ConnectedRpcHostNames.Rpc,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRpcHostNames.Self,
                Nil.Value
            )).Equals(context.Self));
            Term child = await Call(
                ConnectedRpcHostNames.Erpc,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRpcHostNames.Self,
                Nil.Value,
                new MapTerm([new(Term.A(ConnectedRpcHostNames.AlwaysSpawn), Term.A(ErlangBooleanAtoms.True))])
            );
            Require(child is Pid && !child.Equals(context.Self));
            Term reason = Term.Tuple(Term.A(ConnectedRpcHostNames.UserReason), Term.I(42));
            Require((await Call(
                ConnectedRpcHostNames.Rpc,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRpcHostNames.Throw,
                Term.List(reason)
            )).Equals(reason));
            Term exitValue = Term.Tuple(Term.A(ConnectedRpcHostNames.ExitTag), reason);
            Require((await Call(
                ConnectedRpcHostNames.Rpc,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRpcHostNames.Throw,
                Term.List(exitValue)
            ))
                .Equals(Term.Tuple(Term.A(ConnectedRpcHostNames.BadRpc), exitValue)));
            Require((await Call(
                ConnectedRpcHostNames.Rpc,
                ConnectedSpawnHostNames.Erlang,
                ConnectedRpcHostNames.Exit,
                Term.List(reason)
            ))
                .Equals(Term.Tuple(Term.A(ConnectedRpcHostNames.BadRpc), exitValue)));
            try
            {
                await Call(
                    ConnectedRpcHostNames.Erpc,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedRpcHostNames.Exit,
                    Term.List(reason)
                );
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
            }
            catch (ErlangException error)
            {
                Require(error.ExceptionClass == ErlangExceptionClasses.Exit &&
                    error.Reason.Equals(Term.Tuple(Term.A(ConnectedRpcHostNames.Exception), reason)));
            }
            try
            {
                await Call(
                    ConnectedRpcHostNames.Erpc,
                    ConnectedRpcHostNames.Module,
                    ConnectedRpcHostNames.Error,
                    Nil.Value
                );
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
            }
            catch (ErlangException error)
            {
                Require(error.Reason is TupleTerm { Items: [Atom { Name: ConnectedRpcHostNames.Exception }, Atom { Name: ErlangErrorReasons.SystemLimit }, Cons] });
            }
            foreach (int arity in new[] { ConnectedRpcHostNames.AboveFunctionArity, ConnectedRpcHostNames.MaximumApplyArity })
            {
                Term missing = await Call(
                    ConnectedRpcHostNames.Rpc,
                    ConnectedRpcHostNames.Module,
                    ConnectedRpcHostNames.MissingFunction,
                    Cons.From(Enumerable.Repeat<Term>(Nil.Value, arity))
                );
                Require(missing is TupleTerm
                {
                    Items: [Atom { Name: ConnectedRpcHostNames.BadRpc },
                    TupleTerm
                    {
                        Items: [Atom { Name: ConnectedRpcHostNames.ExitTag },
                    TupleTerm { Items: [Atom { Name: ErlangErrorReasons.UndefinedFunction }, Cons] }]
                    }]
                });
            }
            foreach (Term tail in new Term[] { Nil.Value, Term.A(ConnectedRpcHostNames.UserReason) })
            {
                Term overflow = await Call(
                    ConnectedRpcHostNames.Rpc,
                    ConnectedRpcHostNames.Module,
                    ConnectedRpcHostNames.MissingFunction,
                    Cons.From(Enumerable.Repeat<Term>(Nil.Value, ConnectedRpcHostNames.ExcessiveArity), tail)
                );
                Require(overflow.Equals(Term.Tuple(
                    Term.A(ConnectedRpcHostNames.BadRpc),
                    Term.Tuple(Term.A(ConnectedRpcHostNames.ExitTag), Term.A(ErlangErrorReasons.SystemLimit))
                )));
            }
            Term improper = await Call(
                ConnectedRpcHostNames.Rpc,
                ConnectedRpcHostNames.Module,
                ConnectedRpcHostNames.MissingFunction,
                Cons.From(Enumerable.Repeat<Term>(Nil.Value, ConnectedRpcHostNames.MaximumApplyArity), Term.A(ConnectedRpcHostNames.UserReason))
            );
            Require(improper is TupleTerm
            {
                Items: [Atom { Name: ConnectedRpcHostNames.BadRpc },
                TupleTerm
                {
                    Items: [Atom { Name: ConnectedRpcHostNames.ExitTag },
                TupleTerm { Items: [Atom { Name: ErlangErrorReasons.BadArgument }, Cons] }]
                }]
            });
            try
            {
                await Call(
                    ConnectedRpcHostNames.Rpc,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedRpcHostNames.Self,
                    Nil.Value,
                    Term.A(ConnectedRpcHostNames.BadTimeout)
                );
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
            }
            catch (ErlangException error)
            {
                Require(error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            }
            Term disconnected = await runtime.Modules.Call(
                context,
                ConnectedRpcHostNames.Rpc,
                ConnectedRpcHostNames.Call,
                Term.A(ConnectedRpcHostNames.MissingNode),
                Term.A(ConnectedSpawnHostNames.Erlang),
                Term.A(ConnectedRpcHostNames.Self),
                Nil.Value
            );
            Require(disconnected.Equals(Term.Tuple(Term.A(ConnectedRpcHostNames.BadRpc), Term.A(ConnectedRpcHostNames.NodeDown))));

            return true;
        });
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
