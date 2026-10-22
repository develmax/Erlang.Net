using System.Numerics;

namespace Erlang.Tests;

internal static class DebugUnique436HostChecks
{
    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(DebugUnique436HostNames.Failure);
    }

    private static async Task Enable(RuntimeNodeFoundationHostActor actor)
    {
        await actor.InvokeAsync(async context =>
        {
            await context.Runtime.Modules.Call(
                context,
                DebugUnique436HostNames.Runtime,
                DebugUnique436HostNames.Flag,
                Term.A(DebugUnique436HostNames.Logger),
                Term.A(DebugUnique436HostNames.Undefined)
            );
            await DebugInternal420Access.Enabled(context, true);

            return true;
        });
    }

    private static ValueTask<Term> Next(ProcessContext context)
        => context.Runtime.Modules.Call(
            context,
            DebugUnique436HostNames.Runtime,
            DebugUnique436HostNames.Unique,
            Term.List(Term.A(DebugUnique436HostNames.Monotonic), Term.A(DebugUnique436HostNames.Positive))
        );

    internal static async Task Isolation()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        await DebugInternal420Access.Initialize(first);
        await DebugInternal420Access.Initialize(second);
        await using var a = new RuntimeNodeFoundationHostActor(first);
        await using var b = new RuntimeNodeFoundationHostActor(second);
        await Enable(a);
        await Enable(b);
        await a.InvokeAsync(async context =>
        {
            await DebugInternal420Access.Set(context, Term.A(DebugUnique436HostNames.State), Term.I(100));
            Require((await Next(context)).Equals(Term.I(102)));

            return true;
        });
        await b.InvokeAsync(async context =>
        {
            Require((await Next(context)).Equals(Term.I(1)));
            await DebugInternal420Access.Set(context, Term.A(DebugUnique436HostNames.State), Term.I(10));
            Require((await Next(context)).Equals(Term.I(12)));

            return true;
        });
        await a.InvokeAsync(async context =>
        {
            Require((await DebugInternal420Access.Get(context, Term.A(DebugUnique436HostNames.State))).Equals(Term.I(101)));
            Require((await Next(context)).Equals(Term.I(103)));

            return true;
        });
    }

    internal static async Task Parallel()
    {
        await using var runtime = new ProcessRuntime();
        await DebugInternal420Access.Initialize(runtime);
        await using var control = new RuntimeNodeFoundationHostActor(runtime);
        await Enable(control);
        await control.InvokeAsync(async context =>
        {
            await DebugInternal420Access.Set(context, Term.A(DebugUnique436HostNames.State), Term.I(0));

            return true;
        });
        var actors = new List<RuntimeNodeFoundationHostActor>();
        try
        {
            for (int i = 0; i < DebugUnique436HostNames.Actors; i++)
                actors.Add(new(runtime));

            var groups = await Task.WhenAll(actors.Select(actor => actor.InvokeAsync(async context =>
            {
                var values = new List<BigInteger>();
                for (int i = 0; i < DebugUnique436HostNames.PerActor; i++)
                    values.Add(((Integer)await Next(context)).Value);

                return values;
            })));
            BigInteger[] values = groups.SelectMany(group => group).Order().ToArray();
            Require(values.Length == DebugUnique436HostNames.Actors * DebugUnique436HostNames.PerActor);
            for (int i = 0; i < values.Length; i++)
                Require(values[i] == i + 2);

            await control.InvokeAsync(async context =>
            {
                Require(((Integer)await DebugInternal420Access.Get(context, Term.A(DebugUnique436HostNames.State))).Value == values.Length);

                return true;
            });
        }
        finally
        {
            foreach (var actor in actors)
                await actor.DisposeAsync();
        }
    }
}
