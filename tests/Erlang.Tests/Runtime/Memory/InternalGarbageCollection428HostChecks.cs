using System.Reflection;

namespace Erlang.Tests;

internal static class InternalGarbageCollection428HostChecks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(InternalGarbageCollection428HostNames.Failure);
    }

    private static ValueTask<Term> Call(ProcessContext context, Term value)
        => context.Runtime.Modules.Call(
            context,
            InternalGarbageCollection428HostNames.Module,
            InternalGarbageCollection428HostNames.Function,
            value
        );

    private static ushort MinorCount(ProcessContext context)
    {
        var property = typeof(ProcessContext).GetProperty(
            InternalGarbageCollection428HostNames.PolicyProperty, BindingFlags.Instance | BindingFlags.NonPublic)!;
        object policy = property.GetValue(context)!;

        return (ushort)policy.GetType().GetField(
            InternalGarbageCollection428HostNames.MinorField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(policy)!;
    }

    internal static async Task Kernel()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalGarbageCollection428.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            int majorBefore = GC.CollectionCount(GC.MaxGeneration);
            Require((await Call(context, Term.A(InternalGarbageCollection428HostNames.Major))).Equals(Term.A(InternalGarbageCollection428HostNames.True)));
            Require(GC.CollectionCount(GC.MaxGeneration) > majorBefore);
            Require(MinorCount(context) == 0);
            int youngBefore = GC.CollectionCount(0);
            Require((await Call(context, Term.A(InternalGarbageCollection428HostNames.Minor))).Equals(Term.A(InternalGarbageCollection428HostNames.True)));
            Require(GC.CollectionCount(0) > youngBefore);
            Require(MinorCount(context) == 1);
            Require((await Call(context, Term.A(InternalGarbageCollection428HostNames.Major))).Equals(Term.A(InternalGarbageCollection428HostNames.True)));
            Require(MinorCount(context) == 0);

            return Nil.Value;
        });
    }

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.HasExport(InternalGarbageCollection428HostNames.Module,
            InternalGarbageCollection428HostNames.Function, 1));
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapInternalGarbageCollection428.Register(runtime, registry);
            RuntimeBootstrapInternalGarbageCollection428.Register(runtime, registry);
        });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool refused = false;
        try
        {
            RuntimeBootstrapInternalGarbageCollection428.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (Term value in new Term[] { Term.I(0), Nil.Value, Term.A(InternalGarbageCollection428HostNames.True), Term.Tuple() })
            {
                ushort before = MinorCount(context);
                bool rejected = false;
                try
                {
                    await Call(context, value);
                }
                catch (ErlangException error)
                {
                    rejected = error.Reason.Equals(Term.A(InternalGarbageCollection428HostNames.BadArgument));
                }
                Require(rejected && before == MinorCount(context));
            }

            return Nil.Value;
        });
    }
}
