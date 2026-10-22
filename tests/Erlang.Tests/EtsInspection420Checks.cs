using Erlang;
namespace Erlang.Tests;

internal static class EtsInspection420Checks
{
    private static void Require(bool ok)
    {
        if (!ok) throw new InvalidOperationException(EtsInspection420Names.Failure);
    }

    private static void Install(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsFixation420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsInspection420.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string module,
        string function,
        params Term[] args
    ) => runtime.Modules.Call(
        context,
        module,
        function,
        args
    );

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime(1);
        foreach (var signature in EtsInspection420Names.Signatures) Require(!runtime.Modules.HasExport(EtsInspection420Names.Internal, signature.Function, signature.Arity));
        RuntimeBootstrapEtsInspection420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsInspection420.Register(runtime, runtime.Modules);
        foreach (var signature in EtsInspection420Names.Signatures) Require(runtime.Modules.HasExport(EtsInspection420Names.Internal, signature.Function, signature.Arity) && runtime.Modules.SourceOwner(EtsInspection420Names.Internal, signature.Function, signature.Arity) is null);
        await using var foreign = new ProcessRuntime(1);
        bool rejected = false;
        try
        {
            RuntimeBootstrapEtsInspection420.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var rollback = new ProcessRuntime(1);
        var expected = new InvalidOperationException(EtsInspection420Names.Failure);
        try
        {
            await rollback.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapEtsInspection420.Register(rollback, registry);
                throw expected;
            });
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, expected)) { }
        foreach (var signature in EtsInspection420Names.Signatures) Require(!rollback.Modules.HasExport(EtsInspection420Names.Internal, signature.Function, signature.Arity));
    }

    internal static async Task Privilege()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var reader = new RuntimeNodeFoundationHostActor(runtime);
        await using var unrelated = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await owner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInspection420Names.Ets,
            EtsInspection420Names.New,
            Term.A(EtsInspection420Names.Name),
            Term.List(Term.A(EtsInspection420Names.Named), Term.A(EtsInspection420Names.Private))
        ));
        _ = await owner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInspection420Names.Ets,
            EtsInspection420Names.Insert,
            table,
            Term.Tuple(Term.I(1), Term.I(2))
        ));

        async Task<bool> Denied(RuntimeNodeFoundationHostActor actor)
        {
            try
            {
                _ = await actor.InvokeAsync(async context => await Call(
                    runtime,
                    context,
                    EtsInspection420Names.Ets,
                    EtsInspection420Names.Lookup,
                    table,
                    Term.I(1)
                ));

                return false;
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(EtsInspection420Names.Badarg)))
            {
                return true;
            }
        }
        Require(await Denied(reader));
        Require((await reader.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInspection420Names.Internal,
            EtsInspection420Names.Super,
            Term.A(EtsInspection420Names.True)
        ))).Equals(Term.A(EtsInspection420Names.Ok)));
        Require((await reader.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInspection420Names.Ets,
            EtsInspection420Names.Lookup,
            table,
            Term.I(1)
        ))).Equals(Term.List(Term.Tuple(Term.I(1), Term.I(2)))));
        Require(await Denied(unrelated));
        bool invalid = false;
        try
        {
            _ = await reader.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsInspection420Names.Internal,
                EtsInspection420Names.Super,
                Term.A(EtsInspection420Names.Bad)
            ));
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(EtsInspection420Names.Badarg)))
        {
            invalid = true;
        }
        Require(invalid);
        Require((await reader.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInspection420Names.Ets,
            EtsInspection420Names.Lookup,
            table,
            Term.I(1)
        ))).Equals(Term.List(Term.Tuple(Term.I(1), Term.I(2)))));
        _ = await reader.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInspection420Names.Internal,
            EtsInspection420Names.Super,
            Term.A(EtsInspection420Names.False)
        ));
        Require(await Denied(reader));
    }

    internal static async Task Raw()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            async ValueTask<Term> Ets(string function, params Term[] args) => await Call(
                runtime,
                context,
                EtsInspection420Names.Ets,
                function,
                args
            );

            async ValueTask<Term> Internal(string function, params Term[] args) => await Call(
                runtime,
                context,
                EtsInspection420Names.Internal,
                function,
                args
            );
            Term table = await Ets(EtsInspection420Names.New, Term.A(EtsInspection420Names.Name), Nil.Value);
            _ = await Ets(
                EtsInspection420Names.Insert,
                table,
                Term.List(Term.Tuple(Term.I(1), new BitString(new byte[65536])), Term.Tuple(Term.I(2), Term.I(20)))
            );
            _ = await Ets(EtsInspection420Names.Fix, table, Term.A(EtsInspection420Names.True));
            _ = await Ets(EtsInspection420Names.Delete, table, Term.I(1));

            async Task<Term> Keys()
            {
                var keys = new List<Term>();
                Term key = await Internal(EtsInspection420Names.First, table);
                while (!key.Equals(Term.A(EtsInspection420Names.End)))
                {
                    keys.Add(key);
                    key = await Internal(EtsInspection420Names.Next, table, key);
                }

                return Term.List(keys.OrderBy(value => value).ToArray());
            }
            Require((await Keys()).Equals(Term.List(Term.I(1), Term.I(2))));
            Require(await Internal(EtsInspection420Names.Binary, table, Term.I(1)) is Nil);
            _ = await Ets(EtsInspection420Names.Fix, table, Term.A(EtsInspection420Names.False));
            Require((await Keys()).Equals(Term.List(Term.I(2))));
            Require(await Internal(EtsInspection420Names.Binary, table, Term.I(99)) is Nil);

            return true;
        });
    }
}
