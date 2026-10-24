using System.Reflection;

namespace Erlang.Tests;

internal static class MapKeysHost426Checks
{
    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(MapKeysHost426Names.Failure);
    }

    internal static async Task Storage()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapMapKeys426.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var first = Term.A(MapKeysHost426Names.FirstAtom);
        var second = Term.A(MapKeysHost426Names.SecondAtom);
        var key = new TupleTerm([Term.I(7), Term.I(8)]);
        var map = new MapTerm([new(first, key), new(second, Term.I(2)), new(key, Term.I(3))]);
        Term one = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            MapKeysHost426Names.Module,
            MapKeysHost426Names.Function,
            map
        ).AsTask());
        Term two = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            MapKeysHost426Names.Module,
            MapKeysHost426Names.Function,
            map
        ).AsTask());
        Require(ReferenceEquals(one, two));
        var keys = (TupleTerm)one;
        Require(keys.Items.Count == 3 && ReferenceEquals(keys.Items[0], first) && ReferenceEquals(keys.Items[1], second)
            && ReferenceEquals(keys.Items[2], key));
        var empty = new MapTerm([]);
        Term emptyOne = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            MapKeysHost426Names.Module,
            MapKeysHost426Names.Function,
            empty
        ).AsTask());
        Term emptyTwo = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            MapKeysHost426Names.Module,
            MapKeysHost426Names.Function,
            empty
        ).AsTask());
        Require(emptyOne is TupleTerm { Items.Count: 0 } && ReferenceEquals(emptyOne, emptyTwo));
        var copyConstructor = typeof(MapTerm).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(IEnumerable<KeyValuePair<Term, Term>>), typeof(TupleTerm)],
            null
        )!;
        var copy = (MapTerm)copyConstructor.Invoke([map.Entries, keys]);
        Term copiedKeys = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            MapKeysHost426Names.Module,
            MapKeysHost426Names.Function,
            copy
        ).AsTask());
        Require(!ReferenceEquals(copy, map) && ReferenceEquals(copiedKeys, keys));
        bool rejected = false;
        try
        {
            copyConstructor.Invoke([map.Entries, new TupleTerm(keys.Items.Reverse())]);
        }
        catch (TargetInvocationException error) when (error.InnerException is ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
    }

    internal static async Task Ownership()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapMapKeys426.Register(runtime, registry);
            RuntimeBootstrapMapKeys426.Register(runtime, registry);
        });
        var native = typeof(ModuleRegistry).GetMethod(
            MapKeysHost426Names.NativeEntry,
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(string), typeof(string), typeof(int)]
        )!.CreateDelegate<Func<string, string, int, bool>>(runtime.Modules);
        Require(native(MapKeysHost426Names.Module, MapKeysHost426Names.Function, 1) && runtime.Modules.SourceOwner(MapKeysHost426Names.Module, MapKeysHost426Names.Function, 1) is null);
        bool rejected = false;
        try
        {
            RuntimeBootstrapMapKeys426.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        bool cancelled = false;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                DirtyNative419Access.Cancel(context);

                return await runtime.Modules.Call(
                    context,
                    MapKeysHost426Names.Module,
                    MapKeysHost426Names.Function,
                    new MapTerm([])
                );
            });
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        Require(cancelled);
    }
}
