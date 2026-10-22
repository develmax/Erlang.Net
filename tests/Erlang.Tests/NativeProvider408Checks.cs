namespace Erlang.Tests;

internal static class NativeProvider408Checks
{
    internal static async Task DefaultUnchanged()
    {
        await using var plain = new ProcessRuntime();
        // This unchanged existing two-argument default literal must remain unambiguous.
        await using var oldFactory = await ProcessRuntime.CreateAsync(NativeProvider408Access.Empty(), default);
        await using var none = await ProcessRuntime.CreateAsync(NativeProvider408Access.Empty(), RuntimeNativePrimitives.None, CancellationToken.None);
        NativeProvider408Access.Require(plain.Modules.Exports.ToHashSet().SetEquals(oldFactory.Modules.Exports)
            && oldFactory.Modules.Exports.ToHashSet().SetEquals(none.Modules.Exports)
            && plain.NativePrimitives.Bindings.Count == 0 && oldFactory.NativePrimitives.Bindings.Count == 0
            && none.NativePrimitives.Selection == RuntimeNativePrimitives.None);
    }

    internal static async Task ConfiguredActual()
    {
        await using var runtime = new ProcessRuntime();
        await NativeProvider408Access.Enable(runtime);
        NativeProvider408Access.Both(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Task<Term> Call(string name, params Term[] args) => runtime.Modules.Call(
                context,
                NativeProvider408Names.Module,
                name,
                args
            ).AsTask();
            Term key = Term.I(408), value = Term.Tuple(Term.I(1));
            await Call(NativeProvider408Names.Put, key, value);
            NativeProvider408Access.Require((await Call(NativeProvider408Names.Get, key)).Equals(value));
            await Call(NativeProvider408Names.PutNew, key, value);
            bool conflict = false;
            try
            {
                await Call(NativeProvider408Names.PutNew, key, Term.I(2));
            }
            catch (ErlangException error)
            {
                conflict = NativeProvider408Access.NativeError(error, NativeProvider408Names.PutNew);
            }
            NativeProvider408Access.Require(conflict);
            NativeProvider408Access.Require((await Call(NativeProvider408Names.Get)) is Cons);
            NativeProvider408Access.Require((await Call(NativeProvider408Names.Erase, key)).Equals(Term.A(ErlangBooleanAtoms.True)));
            NativeProvider408Access.Require((await Call(NativeProvider408Names.Erase, key)).Equals(Term.A(ErlangBooleanAtoms.False)));
            Term fallback = Term.Tuple(Term.I(99));
            NativeProvider408Access.Require(ReferenceEquals(fallback, await Call(NativeProvider408Names.Get, key, fallback)));
            bool missing = false;
            try
            {
                await Call(NativeProvider408Names.Get, key);
            }
            catch (ErlangException error)
            {
                missing = NativeProvider408Access.NativeError(error, NativeProvider408Names.Get);
            }
            NativeProvider408Access.Require(missing);

            Task<Term> Ets(string name, params Term[] args) => runtime.Modules.Call(
                context,
                NativeProvider408Names.Ets,
                name,
                args
            ).AsTask();
            Term table = await Ets(
                NativeProvider408Names.New,
                Term.A(NativeProvider408Names.Table),
                Cons.From([Term.A(NativeProvider408Names.NamedTable), Term.A(NativeProvider408Names.Public), Term.A(NativeProvider408Names.Set)])
            );
            NativeProvider408Access.Require(await Ets(NativeProvider408Names.Whereis, table) is ReferenceTerm);
            await Ets(NativeProvider408Names.Insert, table, Term.Tuple(Term.I(1), Term.I(2)));
            NativeProvider408Access.Require((await Ets(NativeProvider408Names.Member, table, Term.I(1))).Equals(Term.A(ErlangBooleanAtoms.True)));
            NativeProvider408Access.Require((await Ets(NativeProvider408Names.Lookup, table, Term.I(1))) is Cons);
            Term spec = Cons.From([Term.Tuple(
                Term.Tuple(Term.A(NativeProvider408Names.MatchFirst), Term.A(NativeProvider408Names.MatchSecond)),
                Nil.Value,
                Cons.From([Term.A(NativeProvider408Names.MatchWhole)])
            )]);
            NativeProvider408Access.Require((await Ets(NativeProvider408Names.Select, table, spec)) is Cons);
            await Ets(NativeProvider408Names.Delete, table, Term.I(1));
            NativeProvider408Access.Require((await Ets(NativeProvider408Names.Lookup, table, Term.I(1))) is Nil);
            await Ets(NativeProvider408Names.Delete, table);

            return true;
        });
    }

    internal static async Task InvalidAndForeign()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        var before = runtime.Modules.Exports.ToHashSet();
        bool unknown = false, wrongOwner = false;
        try
        {
            RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, (RuntimeNativePrimitives)4);
        }
        catch (ArgumentOutOfRangeException)
        {
            unknown = true;
        }
        try
        {
            RuntimeNativePrimitiveProviders.Register(runtime, foreign.Modules, NativeProvider408Names.Providers);
        }
        catch (ArgumentException)
        {
            wrongOwner = true;
        }
        NativeProvider408Access.Require(unknown && wrongOwner && before.SetEquals(runtime.Modules.Exports)
            && runtime.NativePrimitives.Bindings.Count == 0 && foreign.NativePrimitives.Bindings.Count == 0);
        await NativeProvider408Access.Enable(runtime);
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        await foreignActor.InvokeAsync(async context =>
        {
            bool persistentRefused = false, etsRefused = false;
            try
            {
                await runtime.Modules.Call(
                    context,
                    NativeProvider408Names.Module,
                    NativeProvider408Names.Get,
                    Term.I(1),
                    Nil.Value
                );
            }
            catch (InvalidOperationException)
            {
                persistentRefused = true;
            }
            try
            {
                await runtime.Modules.Call(
                    context,
                    NativeProvider408Names.Ets,
                    NativeProvider408Names.Whereis,
                    Term.A(NativeProvider408Names.Table)
                );
            }
            catch (ArgumentException)
            {
                etsRefused = true;
            }
            NativeProvider408Access.Require(persistentRefused && etsRefused && foreign.NativePrimitives.Bindings.Count == 0);

            return true;
        });
    }

    internal static async Task ExistingSourceRefused()
    {
        await using var runtime = new ProcessRuntime();
        object owner = new();
        await runtime.InitializeModulesAsync(registry => registry.RegisterSource(
            NativeProvider408Names.Module,
            NativeProvider408Names.Get,
            1,
            (_, _) => ValueTask.FromResult<Term>(Term.A(NativeProvider408Names.PureMarker)),
            owner
        ));
        bool refused = false;
        try
        {
            await NativeProvider408Access.Enable(runtime);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        NativeProvider408Access.Require(refused && runtime.NativePrimitives.Bindings.Count == 0
            && ReferenceEquals(runtime.Modules.SourceOwner(NativeProvider408Names.Module, NativeProvider408Names.Get, 1), owner)
            && !NativeProvider408Access.Has(runtime, NativeProvider408Names.Put, 2)
            && !runtime.Modules.Exports.Any(item => item.Module == NativeProvider408Names.Ets));
    }

    internal static async Task IdempotentAndDriftRefused()
    {
        await using var runtime = new ProcessRuntime();
        await NativeProvider408Access.Enable(runtime);
        var before = runtime.Modules.Exports.ToHashSet();
        await NativeProvider408Access.Enable(runtime);
        NativeProvider408Access.Require(before.SetEquals(runtime.Modules.Exports));
        NativeProvider408Access.Both(runtime);
        runtime.Modules.DeleteModule(NativeProvider408Names.Module);
        runtime.Modules.Register(
            NativeProvider408Names.Module,
            NativeProvider408Names.Get,
            1,
            (_, _) => ValueTask.FromResult<Term>(Term.A(NativeProvider408Names.PureMarker))
        );
        NativeProvider408Access.Require(runtime.NativePrimitives.Bindings.Count == NativeProvider408Names.EtsCount
            && runtime.NativePrimitives.Selection == RuntimeNativePrimitives.EtsStorageCore);
        bool refused = false;
        try
        {
            await NativeProvider408Access.Enable(runtime);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        NativeProvider408Access.Require(refused && runtime.NativePrimitives.Bindings.Count == NativeProvider408Names.EtsCount);
    }

    internal static async Task OuterRollback()
    {
        await using var runtime = new ProcessRuntime();
        var before = runtime.Modules.Exports.ToHashSet();
        var sentinel = new InvalidOperationException(NativeProvider408Names.RollbackMarker);
        bool failed = false;
        try
        {
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeNativePrimitiveProviders.Register(runtime, registry, NativeProvider408Names.Providers);
                throw sentinel;
            });
        }
        catch (InvalidOperationException error)
        {
            failed = ReferenceEquals(error, sentinel);
        }
        NativeProvider408Access.Require(failed && before.SetEquals(runtime.Modules.Exports) && runtime.NativePrimitives.Bindings.Count == 0);
        await NativeProvider408Access.Enable(runtime);
        NativeProvider408Access.Both(runtime);
        // A later package failure must also roll back the earlier package in this same selection.
        await using var occupied = new ProcessRuntime();
        occupied.Modules.Register(
            NativeProvider408Names.Ets,
            NativeProvider408Names.Lookup,
            2,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        bool atomic = false;
        try
        {
            await NativeProvider408Access.Enable(occupied);
        }
        catch (InvalidOperationException)
        {
            atomic = true;
        }
        NativeProvider408Access.Require(atomic && !NativeProvider408Access.Has(occupied, NativeProvider408Names.Get, 1)
            && occupied.NativePrimitives.Bindings.Count == 0);
    }

    internal static async Task WholeStubRefused()
    {
        await using var runtime = new ProcessRuntime();
        await NativeProvider408Access.Enable(runtime);
        var before = runtime.Modules.Exports.ToHashSet();
        string[] declared = System.Text.Json.JsonSerializer.Deserialize<string[]>(LibraryDependencyOriginalHostChecks.ReadRetainedResource(
            LibraryDependencyOriginalNames.Dependencies
        ))!;
        var dependencies = new BeamDependencySelection(declared);
        // The original persistent image now has all seven real native bodies.
        // Preparing it is still nonpublishing; the whole ETS image remains held.
        var persistent = runtime.Modules.PrepareBeam(NativeProvider408Access.Resource(NativeProvider408Names.WholeImage), dependencies);
        NativeProvider408Access.Require(persistent.NativePrimitiveExports.Count == PersistentNative417Names.NativeCount
            && PersistentNative417Names.Signatures.All(signature => persistent.NativePrimitiveExports.Contains(signature))
            && before.SetEquals(runtime.Modules.Exports));
        bool refused = false;
        try
        {
            _ = runtime.Modules.PrepareBeam(NativeProvider408Access.Resource(NativeProvider408Names.EtsImage), dependencies);
        }
        catch (NotSupportedException)
        {
            refused = true;
        }
        NativeProvider408Access.Require(refused && before.SetEquals(runtime.Modules.Exports));
        NativeProvider408Access.Both(runtime);
    }

    internal static async Task Lifecycle()
    {
        var runtime = new ProcessRuntime();
        await NativeProvider408Access.Enable(runtime);
        var writer = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await writer.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                NativeProvider408Names.Module,
                NativeProvider408Names.Put,
                Term.I(1),
                Term.Tuple(Term.I(408))
            ));
            await writer.DisposeAsync();
            await using var reader = new RuntimeNodeFoundationHostActor(runtime);
            await reader.InvokeAsync(async context =>
            {
                NativeProvider408Access.Require((await runtime.Modules.Call(
                    context,
                    NativeProvider408Names.Module,
                    NativeProvider408Names.Get,
                    Term.I(1)
                )).Equals(Term.Tuple(Term.I(408))));
                await runtime.Modules.Call(
                    context,
                    NativeProvider408Names.Module,
                    NativeProvider408Names.Erase,
                    Term.I(1)
                );

                return true;
            });
            await runtime.DisposeAsync();
            bool refused = false;
            try
            {
                RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, NativeProvider408Names.Providers);
            }
            catch (ObjectDisposedException)
            {
                refused = true;
            }
            NativeProvider408Access.Require(refused);
        }
        finally
        {
            await writer.DisposeAsync();
            await runtime.DisposeAsync();
        }
    }

    internal static async Task FactoryGuards()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        bool stopped = false, unknown = false;
        try
        {
            await ProcessRuntime.CreateAsync(NativeProvider408Access.Empty(), NativeProvider408Names.Providers, canceled.Token);
        }
        catch (OperationCanceledException)
        {
            stopped = true;
        }
        try
        {
            await ProcessRuntime.CreateAsync(NativeProvider408Access.Empty(), (RuntimeNativePrimitives)4, CancellationToken.None);
        }
        catch (ArgumentOutOfRangeException)
        {
            unknown = true;
        }
        NativeProvider408Access.Require(stopped && unknown);
        await using var selected = await ProcessRuntime.CreateAsync(NativeProvider408Access.Empty(),
            NativeProvider408Names.Providers, CancellationToken.None);
        NativeProvider408Access.Both(selected);
    }
}
