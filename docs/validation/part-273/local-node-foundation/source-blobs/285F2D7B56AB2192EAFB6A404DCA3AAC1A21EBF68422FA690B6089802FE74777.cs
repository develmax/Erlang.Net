using System.Runtime.CompilerServices;

namespace Erlang.Tests;

public static class RuntimeNodeQueryHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime(RuntimeNodeQueryHostNames.Node);
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                Require((await Call(context, RuntimeNodeQueryNames.IsAlive)).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require((await Call(context, RuntimeNodeQueryNames.Nodes, Term.A(RuntimeNodeQueryNames.This))).Equals(Term.List(runtime.NodeAtom)));
                Term retained = await Call(
                    context,
                    RuntimeNodeQueryHostNames.Decode,
                    new BitString(ExternalTermFormat.Encode(new Pid(RuntimeNodeQueryHostNames.Retained, 1)))
                );
                context.Dictionary[Term.A(RuntimeNodeQueryHostNames.Keep)] = retained;
                var weak = await ObserveTemporary(context);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Require(!weak.TryGetTarget(out _));
                Term known = await Call(context, RuntimeNodeQueryNames.Nodes, Term.A(RuntimeNodeQueryNames.Known));
                Require(Cons.Items(known).Contains(Term.A(RuntimeNodeQueryHostNames.Retained)) && !Cons.Items(known).Contains(Term.A(RuntimeNodeQueryHostNames.Foreign)));
                await using var isolated = new ProcessRuntime();
                var other = isolated.Spawn(async c =>
                {
                    Term nodes = await Call(c, RuntimeNodeQueryNames.Nodes, Term.A(RuntimeNodeQueryNames.Known));
                    Require(!Cons.Items(nodes).Contains(Term.A(RuntimeNodeQueryHostNames.Retained)));

                    return Term.A(ErlangBooleanAtoms.True);
                });
                Require((await other.Completion.WaitAsync(RuntimeNodeQueryHostParameters.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)));
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Term.A(ErlangBooleanAtoms.True);
        });
        await process.Completion.WaitAsync(RuntimeNodeQueryHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference<Term>> ObserveTemporary(ProcessContext context)
    {
        Term decoded = await Call(
            context,
            RuntimeNodeQueryHostNames.Decode,
            new BitString(ExternalTermFormat.Encode(new Pid(RuntimeNodeQueryHostNames.Foreign, 1)))
        );

        return new(decoded);
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] values) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        function,
        values
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RuntimeNodeQueryHostNames.Failure);
    }
}
