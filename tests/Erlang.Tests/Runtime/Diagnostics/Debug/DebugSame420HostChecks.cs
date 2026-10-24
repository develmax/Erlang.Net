using System.Numerics;
namespace Erlang.Tests;

internal static class DebugSame420HostChecks
{
    internal static async Task Source()
    {
        await using var runtime = new ProcessRuntime();
        await DebugSame420HostAccess.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await DebugSame420HostAccess.Same(
                context,
                Term.A(DebugSame420HostNames.Atom),
                Term.A(DebugSame420HostNames.Atom),
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                Term.A(DebugSame420HostNames.Atom),
                Term.A(DebugSame420HostNames.Other),
                false
            );
            await DebugSame420HostAccess.Same(
                context,
                Term.I(42),
                Term.I(42),
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                Nil.Value,
                Nil.Value,
                true
            );
            Term big = new Integer(BigInteger.One << 4096);
            await DebugSame420HostAccess.Same(
                context,
                big,
                big,
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                big,
                new Integer(BigInteger.One << 4096),
                false
            );
            Term tuple = Term.Tuple(Term.I(1));
            await DebugSame420HostAccess.Same(
                context,
                tuple,
                tuple,
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                tuple,
                Term.Tuple(Term.I(1)),
                false
            );
            Term binary = new BitString([1, 2, 3]);
            await DebugSame420HostAccess.Same(
                context,
                binary,
                binary,
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                binary,
                new BitString([1, 2, 3]),
                false
            );
            Term list = Term.List(Term.I(1));
            await DebugSame420HostAccess.Same(
                context,
                list,
                list,
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                list,
                Term.List(Term.I(1)),
                false
            );
            BigInteger magnitude = BigInteger.One << ((IntPtr.Size * 8) - 5);
            await DebugSame420HostAccess.Same(
                context,
                new Integer(magnitude - 1),
                new Integer(magnitude - 1),
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                new Integer(-magnitude),
                new Integer(-magnitude),
                true
            );
            await DebugSame420HostAccess.Same(
                context,
                new Integer(magnitude),
                new Integer(magnitude),
                false
            );
            await DebugSame420HostAccess.Same(
                context,
                new Integer(-magnitude - 1),
                new Integer(-magnitude - 1),
                false
            );
            await DebugSame420HostAccess.Same(
                context,
                context.Self,
                new Pid(context.Self.Node, context.Self.Id, context.Self.Creation),
                true
            );

            return true;
        });
    }

    internal static async Task Owner()
    {
        var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await DebugSame420HostAccess.Initialize(runtime);
        await foreign.InitializeModulesAsync(_ => { });
        ErlangFunction body = DebugSame420HostAccess.Capture(runtime);
        ProcessContext? saved = null;
        try
        {
            await using var other = new RuntimeNodeFoundationHostActor(foreign);
            ProcessContext otherContext = await other.Ready;
            await other.InvokeAsync(async context =>
         {
             bool rejected = false;
             try
             {
                 await body(context, [Nil.Value, Nil.Value]);
             }
             catch (ArgumentException)
             {
                 rejected = true;
             }
             DebugSame420HostAccess.Require(rejected);

             return true;
         });
            await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
            {
                await actor.InvokeAsync(async context =>
                {
                    saved = context;
                    await DebugSame420HostAccess.Same(
                        context,
                        context.Self,
                        otherContext.Self,
                        false
                    );
                    // Wider hosting IDs must not inherit the deliberate truncation allowed
                    // by the hash protocol. They are distinct actual own identifier values.
                    PortTerm a = new(context.Self.Node, 1, context.Self.Creation), b = new(context.Self.Node, 1UL + (1UL << 60), context.Self.Creation);
                    await DebugSame420HostAccess.Same(
                        context,
                        a,
                        b,
                        false
                    );
                    await DebugSame420HostAccess.Same(
                        context,
                        a,
                        new PortTerm(context.Self.Node, 1, context.Self.Creation),
                        true
                    );
                    DebugLocks419Access.Cancel(context);
                    bool canceled = false;
                    try
                    {
                        await body(context, [Nil.Value, Nil.Value]);
                    }
                    catch (OperationCanceledException)
                    {
                        canceled = true;
                    }
                    DebugSame420HostAccess.Require(canceled);

                    return true;
                });
            }
            await runtime.DisposeAsync();
            bool closed = false;
            try
            {
                await body(saved!, [Nil.Value, Nil.Value]);
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            DebugSame420HostAccess.Require(closed);
        }
        finally
        {
            await runtime.DisposeAsync();
        }
    }
}
