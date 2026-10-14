namespace Erlang.Tests;

public static class RuntimeNodeControlHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime(RuntimeNodeControlHostNames.Node);
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                RuntimeNodeAdmissionHostSetup.Attach(context, RuntimeNodeControlHostNames.Node);
                Term peer = Term.A(RuntimeNodeControlHostNames.Peer);
                foreach (Term options in new Term[] { Nil.Value, Term.List(Term.A(RuntimeNodeControlNames.AllowPassive)) })
                {
                    Require((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        RuntimeNodeControlNames.Monitor,
                        peer,
                        Term.A(ErlangBooleanAtoms.True),
                        options
                    )).Equals(Term.A(ErlangBooleanAtoms.True)));
                    var message = await context.ReceiveAsync(
                        value => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(Term.A(RuntimeNodeControlNames.NodeDown)) && tuple.Items[1].Equals(peer) ? tuple : null,
                        RuntimeNodeControlHostNames.Timeout
                    );
                    Require(message is not null);
                }
                foreach (Term node in new[] { peer, runtime.NodeAtom })
                    Require((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        RuntimeNodeControlNames.Monitor,
                        node,
                        Term.A(ErlangBooleanAtoms.False)
                    )).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    RuntimeNodeControlNames.Monitor,
                    runtime.NodeAtom,
                    Term.A(ErlangBooleanAtoms.True)
                )).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require(await context.ReceiveAsync(value => value, TimeSpan.Zero) is null);
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    RuntimeNodeControlNames.Disconnect,
                    peer
                )).Equals(Term.A(RuntimeNodeControlNames.Ignored)));
                Require((await runtime.Modules.Call(context, ErlangModuleNames.Module, RuntimeNodeQueryNames.Nodes)).Equals(Nil.Value));
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Term.A(ErlangBooleanAtoms.True);
        });
        await process.Completion.WaitAsync(RuntimeNodeControlHostNames.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RuntimeNodeControlHostNames.Failure);
    }
}
