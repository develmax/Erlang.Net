using System.Collections;
using System.Reflection;
namespace Erlang.Tests;

internal static class DistDecode423Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DistDecode423Names.Failure);
    }

    private static Task<Term> Decode(RuntimeNodeFoundationHostActor actor, TupleTerm cache, byte[] bytes)
        => actor.InvokeAsync(context => context.Runtime.Modules.Call(
            context,
            DistDecode423Names.Module,
            DistDecode423Names.Function,
            cache,
            new BitString(bytes)
        ).AsTask());

    private static async Task Bad(Func<Task> action)
    {
        bool caught = false;
        try
        {
            await action();
        }
        catch (ErlangException error)
        {
            caught = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
        }
        Require(caught);
    }

    internal static async Task Cache()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDistDecode423.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(runtime);
        TupleTerm first = Term.Tuple(Term.A(DistDecode423Names.First)), second = Term.Tuple(Term.A(DistDecode423Names.Second));
        Require((await Decode(actor, first, DistDecode423Bytes.CacheZero)).Equals(first.Items[0]));
        Require((await Decode(actor, second, DistDecode423Bytes.CacheZero)).Equals(second.Items[0]));
        for (int index = 0; index < 16; index++)
        {
            Term[] pair = await Task.WhenAll(Decode(actor, first, DistDecode423Bytes.CacheZero), Decode(other, second, DistDecode423Bytes.CacheZero));
            Require(pair[0].Equals(first.Items[0]) && pair[1].Equals(second.Items[0]));
        }
        TupleTerm boundary = Term.Tuple(Enumerable.Repeat<Term>(Term.A(DistDecode423Names.Last), 255).ToArray());
        Require((await Decode(actor, boundary, DistDecode423Bytes.CacheLast)).Equals(Term.A(DistDecode423Names.Last)));
        await Bad(async () =>
 {
     await Decode(actor, boundary, DistDecode423Bytes.CacheTooFar);
 });
        await Bad(() =>
 {
     ExternalTermFormat.Decode(DistDecode423Bytes.CacheZero);

     return Task.CompletedTask;
 });
        await Bad(() =>
 {
     runtime.ImportExternalTerm(DistDecode423Bytes.CacheZero);

     return Task.CompletedTask;
 });
        await Bad(async () =>
 {
     await actor.InvokeAsync(context => runtime.Modules.Call(
         context,
         DistDecode423Names.Runtime,
         DistDecode423Names.Decode,
         new BitString(DistDecode423Bytes.CacheZero)
     ).AsTask());
 });
    }

    internal static async Task Epoch()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDistDecode423.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        TupleTerm cache = Term.Tuple(Term.A(DistDecode423Names.Remote));
        int before = IdentifierLifetimeHostAccess.IdentityCount(runtime);
        await Bad(async () =>
 {
     await Decode(actor, cache, DistDecode423Bytes.InvalidNode);
 });
        Require(IdentifierLifetimeHostAccess.IdentityCount(runtime) == before);
        Term pid = await Decode(actor, cache, DistDecode423Bytes.Pid);
        Term port = await Decode(actor, cache, DistDecode423Bytes.Port);
        Term reference = await Decode(actor, cache, DistDecode423Bytes.Reference);
        object? identity = IdentifierLifetimeHostAccess.Observed(runtime, pid);
        Require(IdentifierLifetimeHostAccess.IsIdentity(identity)
            && ReferenceEquals(identity, IdentifierLifetimeHostAccess.Observed(runtime, port))
            && ReferenceEquals(identity, IdentifierLifetimeHostAccess.Observed(runtime, reference)));
        object known = IdentifierLifetimeHostAccess.Known(runtime);
        Require((bool)known.GetType().GetMethod(DistDecode423Names.KnownEpoch, IdentifierLifetimeHostNames.InstanceFlags)!.Invoke(known, [DistDecode423Names.Remote, (uint)7])!);
        IdentifierLifetimeHostAccess.Collect();
        Require(ReferenceEquals(identity, IdentifierLifetimeHostAccess.Observed(runtime, pid)));
        GC.KeepAlive(port);
        GC.KeepAlive(reference);
        GC.KeepAlive(pid);
    }

    internal static async Task Local()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDistDecode423.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require(await Decode(actor, Term.Tuple(), DistDecode423Bytes.StaticNormalFun) is FunctionTerm { Arity: 1 });
        await Bad(async () =>
 {
     await Decode(actor, Term.Tuple(), DistDecode423Bytes.StaticLocalFun);
 });
        await actor.InvokeAsync(async context =>
        {
            Term reference = await runtime.Modules.Call(context, DistDecode423Names.Runtime, DistDecode423Names.MakeReference);
            Term original = Term.Tuple(context.Self, reference);
            BitString binary = (BitString)await runtime.Modules.Call(
                context,
                DistDecode423Names.Runtime,
                DistDecode423Names.Encode,
                original,
                Term.List(Term.A(DistDecode423Names.Local))
            );
            Term decoded = await runtime.Modules.Call(
                context,
                DistDecode423Names.Module,
                DistDecode423Names.Function,
                Term.Tuple(),
                binary
            );
            Require(decoded.Equals(original));
            Term repeated = await runtime.Modules.Call(
                context,
                DistDecode423Names.Module,
                DistDecode423Names.Function,
                Term.Tuple(),
                new BitString(binary.ToArray().Concat(DistDecode423Bytes.Trailing).ToArray())
            );
            Require(repeated.Equals(original));

            return true;
        });
    }

    internal static async Task RawAtom()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDistDecode423.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        using var atomScope = runtime.EnterAtomScope();
        int anchor = AtomNameRegistry.IndexOf(DistDecode423Names.RawAtomAnchor);
        Require(anchor <= ushort.MaxValue);
        foreach (bool wide in new[] { false, true })
        {
            byte[] wire = DistDecode423Bytes.InternalAtom(anchor, wide);
            Require((await Decode(actor, Term.Tuple(), wire)).Equals(Term.A(DistDecode423Names.RawAtomAnchor)));
            await Bad(() =>
 {
     ExternalTermFormat.Decode(wire);

     return Task.CompletedTask;
 });
            await Bad(() =>
 {
     runtime.ImportExternalTerm(wire);

     return Task.CompletedTask;
 });
        }
        await Bad(async () =>
 {
     await Decode(actor, Term.Tuple(), DistDecode423Bytes.InternalAtom(DistDecode423Bytes.UnknownInternalAtomIndex, true));
 });
        await Task.WhenAll(Enumerable.Range(0, 32).Select(index => Task.Run(async () =>
        {
            string name = DistDecode423Names.RawAtomPrefix + index;
            Term atom = Term.A(name);
            int assigned = AtomNameRegistry.IndexOf(name);
            Require((await Decode(actor, Term.Tuple(), DistDecode423Bytes.InternalAtom(assigned, true))).Equals(atom));
        })));
    }

    internal static async Task Owner()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
 {
     RuntimeBootstrapDistDecode423.Register(runtime, registry);
     RuntimeBootstrapDistDecode423.Register(runtime, registry);
 });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        ProcessContext context = await actor.Ready, alien = await other.Ready;
        var functions = (IDictionary)typeof(ModuleRegistry).GetField(DistDecode423Names.Functions, IdentifierLifetimeHostNames.InstanceFlags)!.GetValue(runtime.Modules)!;
        var body = (ErlangFunction)functions[(DistDecode423Names.Module, DistDecode423Names.Function, 2)]!;
        var nativeEntry = typeof(ModuleRegistry).GetMethod(
            DistDecode423Names.NativeEntry,
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(string), typeof(string), typeof(int)]
        )!.CreateDelegate<Func<string, string, int, bool>>(runtime.Modules);
        Require(nativeEntry(DistDecode423Names.Module, DistDecode423Names.Function, 2));
        bool refused = false;
        try
        {
            await body(alien, [Term.Tuple(), new BitString(DistDecode423Bytes.CacheZero)]);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        Require(await actor.InvokeAsync(async current =>
        {
            DirtyNative419Access.Cancel(current);
            try
            {
                await body(current, [Term.Tuple(), new BitString(DistDecode423Bytes.CacheZero)]);
            }
            catch (OperationCanceledException)
            {
                return true;
            }

            return false;
        }));
        await actor.DisposeAsync();
        await runtime.DisposeAsync();
        bool closed = false;
        try
        {
            await body(context, [Term.Tuple(), new BitString(DistDecode423Bytes.CacheZero)]);
        }
        catch (ObjectDisposedException)
        {
            closed = true;
        }
        Require(closed);
    }
}
