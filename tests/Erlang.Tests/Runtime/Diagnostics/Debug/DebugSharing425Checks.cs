namespace Erlang.Tests;

internal static class DebugSharing425Checks
{
    private static void Require(bool value) => DebugSharing425Access.Require(value);

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => DebugSharing425Access.Call(context, function, arguments);

    internal static async Task Graph()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            TupleTerm empty = Term.Tuple();
            var anotherEmpty = new TupleTerm(Enumerable.Empty<Term>());
            var encodedEmpty = await context.Runtime.Modules.Call(
                context,
                DebugSharing425Names.RuntimeModule,
                DebugSharing425Names.Encode,
                empty
            );
            var decodedEmpty = (TupleTerm)await context.Runtime.Modules.Call(
                context,
                DebugSharing425Names.RuntimeModule,
                DebugSharing425Names.Decode,
                encodedEmpty
            );
            Require(!ReferenceEquals(empty, anotherEmpty) && !ReferenceEquals(empty, decodedEmpty)
                && ReferenceEquals(empty.Items, anotherEmpty.Items) && ReferenceEquals(empty.Items, decodedEmpty.Items)
                && DebugSharing425Access.Same(context, empty, anotherEmpty)
                && DebugSharing425Access.Same(context, empty, decodedEmpty));
            Require((await Call(context, DebugSharing425Names.Flat, empty)).Equals(Term.I(0))
                && (await Call(context, DebugSharing425Names.Shared, anotherEmpty)).Equals(Term.I(0)));
            foreach (string flag in new[] { DebugSharing425Names.FalseAtom, DebugSharing425Names.TrueAtom })
            {
                var emptyCopy = (TupleTerm)await Call(
                    context,
                    DebugSharing425Names.Copy,
                    empty,
                    Term.A(flag)
                );
                Require(ReferenceEquals(empty.Items, emptyCopy.Items)
                    && DebugSharing425Access.Same(context, empty, emptyCopy));
            }
            var nonempty = Term.Tuple(Term.I(0));
            var equalNonempty = Term.Tuple(Term.I(0));
            Require(nonempty.Equals(equalNonempty) && !ReferenceEquals(nonempty.Items, equalNonempty.Items)
                && !DebugSharing425Access.Same(context, nonempty, equalNonempty));
            var emptyMap = new MapTerm([]);
            Require(ReferenceEquals(DebugSharing425Access.Keys(emptyMap).Items, empty.Items)
                && (await Call(context, DebugSharing425Names.Flat, emptyMap)).Equals(Term.I(3)));
            var child = Term.Tuple(Term.I(42), Term.I(43));
            Term source = Term.Tuple(child, child);
            Require((await Call(context, DebugSharing425Names.Flat, source)).Equals(Term.I(9)));
            Require((await Call(context, DebugSharing425Names.Shared, source)).Equals(Term.I(6)));
            var copy = (TupleTerm)await Call(
                context,
                DebugSharing425Names.Copy,
                source,
                Term.A("true")
            );
            Require(copy.Equals(source) && !ReferenceEquals(source, copy) && ReferenceEquals(copy.Items[0], copy.Items[1])
                && !ReferenceEquals(copy.Items[0], child));
            var map = new MapTerm([new(Term.A("a"), child), new(Term.A("b"), child)]);
            TupleTerm keys = DebugSharing425Access.Keys(map);
            var graph = Term.Tuple(map, keys);
            var graphCopy = (TupleTerm)await Call(
                context,
                DebugSharing425Names.Copy,
                graph,
                Term.A("true")
            );
            var mapCopy = (MapTerm)graphCopy.Items[0];
            Require(ReferenceEquals(DebugSharing425Access.Keys(mapCopy), graphCopy.Items[1])
                && !ReferenceEquals(keys, graphCopy.Items[1]) && ReferenceEquals(mapCopy.Get(Term.A("a")), mapCopy.Get(Term.A("b"))));
            Term deep = Nil.Value;
            for (int index = 0; index < 8192; index++) deep = new Cons(Term.I(index), deep);
            Require((await Call(context, DebugSharing425Names.Flat, deep)).Equals(Term.I(16384)));
            Require(Cons.Items(await Call(
                context,
                DebugSharing425Names.Copy,
                deep,
                Term.A("false")
            )).Count() == 8192);

            return true;
        });
    }

    internal static async Task Storage()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var resource = runtime.ReferenceResources.CreateBinary(new byte[] { 4, 5, 6 });
            var binary = new BitString(new byte[128]);
            object backing = DebugSharing425Access.Backing(binary), entry = DebugSharing425Access.Resource(resource);
            Term source = Term.Tuple(
                resource,
                resource,
                binary,
                binary
            );
            var copy = (TupleTerm)await Call(
                context,
                DebugSharing425Names.Copy,
                source,
                Term.A("true")
            );
            Require(!ReferenceEquals(copy.Items[0], resource) && ReferenceEquals(copy.Items[0], copy.Items[1])
                && ReferenceEquals(DebugSharing425Access.Resource((ReferenceTerm)copy.Items[0]), entry));
            using var lease = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)copy.Items[0]);
            Require(lease is not null && lease.Value.Equals(new BitString(new byte[] { 4, 5, 6 })));
            Require(!ReferenceEquals(copy.Items[2], binary) && ReferenceEquals(copy.Items[2], copy.Items[3])
                && ReferenceEquals(DebugSharing425Access.Backing((BitString)copy.Items[2]), backing));

            return true;
        });
    }

    internal static async Task Functions()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm canonical = runtime.Functions.External(runtime, new(Term.A("erlang"), Term.A("self"), 0));
            var source = (FunctionTerm)await Call(
                context,
                DebugSharing425Names.Copy,
                canonical,
                Term.A(ExportGlobal431Names.True)
            );
            Require(!ReferenceEquals(source, canonical) && !runtime.Functions.IsExternalEntry(source));
            var copy = (TupleTerm)await Call(
                context,
                DebugSharing425Names.Copy,
                Term.Tuple(source, source),
                Term.A("true")
            );
            Require(!ReferenceEquals(copy.Items[0], source) && ReferenceEquals(copy.Items[0], copy.Items[1]) && copy.Items[0].Equals(source));
            Require((await ((FunctionTerm)copy.Items[0]).Invoke(context, [])).Equals(context.Self));
            var repeated = (FunctionTerm)await Call(
                context,
                DebugSharing425Names.Copy,
                copy.Items[0],
                Term.A("false")
            );
            Require(!ReferenceEquals(repeated, copy.Items[0]) && (await repeated.Invoke(context, [])).Equals(context.Self));

            return true;
        });
    }

    internal static async Task Source()
    {
        var failures = new List<Exception>();
        foreach (bool privateProfile in new[] { false, true })
            try
            {
                await using ProcessRuntime runtime = await DebugSharing425Access.SourceRuntime(privateProfile);
                await DebugGuestIdentity377Checks.Prepare(runtime);
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                await DebugGuestIdentity377Checks.SourceCase(runtime, actor);
                await DebugGuestIdentity377Checks.OpaqueBoundaries(runtime, actor, privateProfile);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        try
        {
            await DebugGuestCounter377Checks.Run();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count != 0) throw new AggregateException(DebugSharing425Names.Failure, failures);
    }

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDebugSharing425.Register(runtime, registry);
            RuntimeBootstrapDebugSharing425.Register(runtime, registry);
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            bool failed = false;
            try
            {
                await Call(
                    context,
                    DebugSharing425Names.Copy,
                    Term.I(42),
                    Term.I(1)
                );
            }
            catch (ErlangException error)
            {
                var first = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
                failed = error.Reason.Equals(Term.A("badarg")) && first.Items[0].Equals(Term.A(DebugSharing425Names.Module))
                    && first.Items[1].Equals(Term.A(DebugSharing425Names.Copy)) && first.Items[2].Equals(Term.List(Term.I(42), Term.I(1)));
            }
            Require(failed);

            return true;
        });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool refused = false;
        try
        {
            RuntimeBootstrapDebugSharing425.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
    }
}
