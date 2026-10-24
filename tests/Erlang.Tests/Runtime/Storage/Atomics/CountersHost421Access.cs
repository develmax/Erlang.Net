using Erlang;
namespace Erlang.Tests;

internal static class CountersHost421Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(CountersHost421Names.Failure);
    }

    internal static async Task<ProcessRuntime> Create()
    {
        var runtime = new ProcessRuntime(CountersHost421Names.Lanes);
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                var facet = CountersOriginal421Preparation.Prepare(runtime, registry);
                Require(registry.PublishPreparedBeam(facet).Status == BeamPublicationStatus.Published);
                CountersOriginal421Preparation.AssertPublished(registry, facet);
            });

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    internal static Task<Term> Call(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        string function,
        params Term[] arguments
    )
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            CountersHost421Names.Internal,
            function,
            arguments
        ));

    internal static async Task BadArgument(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        string function,
        params Term[] arguments
    )
    {
        bool refused = false;
        try
        {
            _ = await Call(
                actor,
                runtime,
                function,
                arguments
            );
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            refused = true;
        }
        Require(refused);
    }

    internal static IDisposable Hold(ProcessRuntime runtime, Term reference, bool invocation)
    {
        if (invocation)
        {
            object owner = typeof(ProcessRuntime).GetField(CountersHost421Names.Owner, CountersHost421Names.Members)!.GetValue(runtime)!;

            return (IDisposable)owner.GetType().GetMethod(CountersHost421Names.Enter, CountersHost421Names.Members)!.Invoke(owner, null)!;
        }
        object entry = typeof(ResourceReferenceTerm).GetProperty(CountersHost421Names.Entry, CountersHost421Names.Members)!.GetValue(reference)!;
        Type rootType = typeof(ProcessRuntime).Assembly.GetType(CountersHost421Names.RootType)!;
        object root = entry.GetType().GetMethod(CountersHost421Names.Read, CountersHost421Names.Members)!.MakeGenericMethod(rootType).Invoke(entry, null)!;
        object array = rootType.GetProperty(CountersHost421Names.Array, CountersHost421Names.Members)!.GetValue(root)!;

        return (IDisposable)array.GetType().GetMethod(CountersHost421Names.Acquire, CountersHost421Names.Members)!.Invoke(array, null)!;
    }
}
