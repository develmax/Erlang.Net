using System.Reflection;

namespace Erlang.Tests;

internal static class BeamfileMd5Host423Checks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(BeamfileMd5Host423Names.Failure);
    }

    private static Task<Term> Read(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, Term input)
        => actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            BeamfileMd5Host423Names.Module,
            BeamfileMd5Host423Names.Function,
            input
        ).AsTask());

    internal static async Task Raw()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapBeamfileMd5423.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        byte[] image = BeamfileMd5Host423Bytes.Image();
        Term original = await Read(runtime, actor, new BitString(image));
        Require(original is BitString { BitLength: BeamfileMd5Host423Names.HashBits });
        // These unsupported opcode bytes are intentionally not executed or linked.
        byte[] prefixed = [0, .. image];
        Require((await Read(runtime, actor, new BitString(prefixed).SliceBits(8, image.Length * 8))).Equals(original));
        Require((await Read(runtime, actor, new BitString([.. image, 255]))).Equals(original));
        Require((await Read(
            runtime,
            actor,
            new BitString(BeamfileMd5Host423Bytes.Image((BeamfileMd5Host423Names.Lines, BeamfileMd5Host423Bytes.Words(999))))
        )).Equals(original));
        Require((await Read(
            runtime,
            actor,
            new BitString(BeamfileMd5Host423Bytes.Image((BeamfileMd5Host423Names.Lines, BeamfileMd5Host423Bytes.Words(0))))
        )).Equals(Term.A(BeamfileMd5Host423Names.Undefined)));
        Require((await Read(
            runtime,
            actor,
            new BitString(BeamfileMd5Host423Bytes.Image((BeamfileMd5Host423Names.Records, BeamfileMd5Host423Bytes.Words(1))))
        )).Equals(Term.A(BeamfileMd5Host423Names.Undefined)));
        bool badarg = false;
        try
        {
            await Read(runtime, actor, Term.I(1));
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            badarg = true;
        }
        Require(badarg);
    }

    internal static async Task Literals()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapBeamfileMd5423.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        FunctionTerm function = (FunctionTerm)runtime.ImportExternalTerm(DistDecode423Bytes.StaticNormalFun).Term;
        LocalFunctionIdentity identity = function.LocalIdentity!;
        int calls = 0;
        runtime.Functions.Register(
            identity,
            function.Arity,
            0,
            environment =>
        {
            Interlocked.Increment(ref calls);

            return new FunctionTerm(
                identity,
                function.Arity,
                environment,
                (_, arguments) => ValueTask.FromResult(arguments[0])
            );
        }
        );
        byte[] literal = [.. BeamfileMd5Host423Bytes.Words(0, 1, DistDecode423Bytes.StaticNormalFun.Length), .. DistDecode423Bytes.StaticNormalFun];
        Term hash = await Read(runtime, actor, new BitString(BeamfileMd5Host423Bytes.Image((BeamfileMd5Host423Names.Literals, literal))));
        Require(hash is BitString { BitLength: BeamfileMd5Host423Names.HashBits } && calls == 0);
        Term value = await actor.InvokeAsync(context => function.Invoke(context, [Term.I(42)]).AsTask());
        Require(value.Equals(Term.I(42)) && calls == 1);

        int index = AtomNameRegistry.IndexOf(BeamfileMd5Host423Names.LiteralAtom);
        byte[] raw = DistDecode423Bytes.InternalAtom(index, index > ushort.MaxValue);
        literal = [.. BeamfileMd5Host423Bytes.Words(0, 1, raw.Length), .. raw];
        Require(await Read(runtime, actor, new BitString(BeamfileMd5Host423Bytes.Image((BeamfileMd5Host423Names.Literals, literal))))
            is BitString { BitLength: BeamfileMd5Host423Names.HashBits });
        bool denied = false;
        try
        {
            ExternalTermFormat.Decode(raw);
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            denied = true;
        }
        Require(denied);
        byte[] cache = DistDecode423Bytes.CacheZero;
        literal = [.. BeamfileMd5Host423Bytes.Words(0, 1, cache.Length), .. cache];
        Require((await Read(runtime, actor, new BitString(BeamfileMd5Host423Bytes.Image((BeamfileMd5Host423Names.Literals, literal)))))
            .Equals(Term.A(BeamfileMd5Host423Names.Undefined)));
    }

    internal static async Task Owner()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapBeamfileMd5423.Register(runtime, registry);
            RuntimeBootstrapBeamfileMd5423.Register(runtime, registry);
        });
        var native = typeof(ModuleRegistry).GetMethod(
            BeamfileMd5Host423Names.NativeEntry,
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(string), typeof(string), typeof(int)]
        )!
            .CreateDelegate<Func<string, string, int, bool>>(runtime.Modules);
        Require(native(BeamfileMd5Host423Names.Module, BeamfileMd5Host423Names.Function, 1));
        Require(runtime.Modules.SourceOwner(BeamfileMd5Host423Names.Module, BeamfileMd5Host423Names.Function, 1) is null);
        bool rejected = false;
        try
        {
            RuntimeBootstrapBeamfileMd5423.Register(runtime, foreign.Modules);
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
                    BeamfileMd5Host423Names.Module,
                    BeamfileMd5Host423Names.Function,
                    new BitString(BeamfileMd5Host423Bytes.Image())
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
