using System.Collections;
using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NoBackingClosure192Names;
using static Erlang.Tests.PublishedFunctionClosure293Names;

namespace Erlang.Tests;

internal static class PublishedFunctionClosure293Checks
{
    internal static async Task Run()
    {
        await ActualFallbackEnvironments464();
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
                object heap = Property(context, Heap);
                using var frame = (IDisposable)Call(heap, Enter)!;
                int callbacks = 0;
                FunctionTerm source = Source([Nil.Value], () => callbacks++);
                // The ACTUAL import pipeline owns construction roots and creates
                // the real dispatch entry. No Published bit/authority is forged.
                using var import = (IDisposable)Call(frame, PrepareImport293, source)!;
                var function = (FunctionTerm)Property(import, Value293);
                object block = Property(function, FunctionStorage293);
                object entry = ((IDictionary)Field(heap, Entries293))[Property(block, Identity293)]!;
                object staging = Field(import, Roots293);
                object custody = Field(staging, Custody293);
                Term beforeParent;
                lock (Property(heap, Gate293))
                {
                    Require(!(bool)Property(entry, Published293));
                    Flags(
                        block,
                        true,
                        true,
                        true
                    );
                    Require(!(bool)Call(
                        heap,
                        UseProof293,
                        function,
                        block
                    )!);
                    beforeParent = (Term)Call(
                        heap,
                        CreateCons293,
                        function,
                        Nil.Value
                    )!;
                    Call(custody, Publish293, beforeParent);
                    Flags(
                        StorageOf(heap, beforeParent),
                        false,
                        false,
                        false
                    );
                    using (var pending = (IDisposable)Call(
                        heap,
                        NativePrepare293,
                        beforeParent,
                        true
                    )!)
                        Require(!(bool)Property(entry, Published293));
                    // Disposing uncommitted native publication has NOT promoted.
                    Require(!(bool)Property(entry, Published293)
                        && !(bool)Call(
                            heap,
                            UseProof293,
                            function,
                            block
                        )!);
                    using (var pending = (IDisposable)Call(
                        heap,
                        NativePrepare293,
                        beforeParent,
                        true
                    )!)
                    {
                        Call(pending, NativeCommit293);
                        Require((bool)Property(entry, Published293));
                    }
                    Require((bool)Call(
                        heap,
                        UseProof293,
                        function,
                        block
                    )!);
                }
                // Actual frame publication still executes; the previously
                // uncertified parent never retroactively changes its flags.
                Call(
                    frame,
                    Set,
                    (int)Call(frame, Reserve)!,
                    beforeParent
                );
                Flags(
                    StorageOf(heap, beforeParent),
                    false,
                    false,
                    false
                );
                long small = Accumulate(
                    runtime,
                    heap,
                    frame,
                    function,
                    Small293
                );
                long large = Accumulate(
                    runtime,
                    heap,
                    frame,
                    function,
                    Large293
                );
                Require(small == Small293 && large == Large293 && callbacks == 0);
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                {
                    FunctionClosure293 = true,
                    PrivateProfile = owned,
                    SmallCount = Small293,
                    SmallActualVisits = small,
                    LargeCount = Large293,
                    LargeActualVisits = large,
                    PublishedThroughRealNativeCommit = true,
                    CallbackCount = callbacks
                }));

                return true;
            });
        }
    }

    private static async Task ActualFallbackEnvironments464()
    {
        foreach (bool profile in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(profile);
            await runtime.InitializeModulesAsync(_ => { });
            ModuleDefinition template = new Parser(FallbackSource464).ParseModule();
            await using PreparedSourceModule819 prepared = template.PrepareSourceModule819(runtime.Modules);
            await runtime.InitializeModulesAsync(_ => prepared.Register());
            ModuleCodeImage image = runtime.Modules.CurrentCodeImage(FallbackModule464)!;
            object domain = Property(image, SourceLiteral819Names.ImageDomain);
            Task retirement = (Task)Property(domain, SourceLiteral819Names.Retirement);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            FunctionTerm? globalOuter = null, literalOuter = null;
            Term? actualLiteral = null;
            var expected = Term.Tuple(Term.A(LiteralAtom464), Term.List(Term.I(17), Term.I(23)));
            await actor.InvokeAsync(async context =>
            {
                object heap = Property(context, Heap);
                _ = await Execution.EvaluateAsync(new Expr.Call(FallbackModule464, KeepGlobal464, []), context);
                globalOuter = StoredFunction464(context);
                var inner = (FunctionTerm)globalOuter.Environment.Single();
                var actualGlobal = (FunctionTerm)inner.Environment.Single();
                FunctionTerm exactGlobal = runtime.Functions.External(runtime,
                    new ExternalFunctionIdentity(Term.A(ErlangModuleNames.Module), Term.A(Abs464), 1));
                Require(ReferenceEquals(actualGlobal, exactGlobal));
                Require(!(bool)Property(Property(inner, FunctionStorage293), LiteralOnly293)
                    && !(bool)Property(Property(globalOuter, FunctionStorage293), LiteralOnly293));
                Call(
                    heap,
                    SourceLiteral819Names.Collect,
                    true,
                    (ushort)0
                );
                Term called = await Execution.EvaluateAsync(new Expr.Call(FallbackModule464, CallGlobal464, []), context);
                Require(called.Equals(Term.I(464)) && ReferenceEquals(inner.Environment.Single(), exactGlobal));
                Require(context.Dictionary.Remove(Term.A(FallbackKey464)));
                Call(
                    heap,
                    SourceLiteral819Names.Collect,
                    true,
                    (ushort)0
                );
                RequireStaleFunction464(globalOuter);
                Require(runtime.Functions.IsExternalEntry(exactGlobal));

                _ = await Execution.EvaluateAsync(new Expr.Call(FallbackModule464, KeepLiteral464, []), context);
                literalOuter = StoredFunction464(context);
                var literalInner = (FunctionTerm)literalOuter.Environment.Single();
                actualLiteral = literalInner.Environment.Single();
                object literalStorage = Property(actualLiteral, SourceLiteral819Names.Storage);
                Require(ReferenceEquals(Property(Property(literalStorage, SourceLiteral819Names.StorageOwner), SourceLiteral819Names.HeapDomain), domain));
                Require((bool)Property(Property(literalInner, FunctionStorage293), LiteralOnly293)
                    && !(bool)Property(Property(literalInner, FunctionStorage293), EmptyCapture293)
                    && !(bool)Property(Property(literalOuter, FunctionStorage293), EmptyCapture293));
                Call(
                    heap,
                    SourceLiteral819Names.Collect,
                    true,
                    (ushort)0
                );
                Term returned = await Execution.EvaluateAsync(new Expr.Call(FallbackModule464, CallLiteral464, []), context);
                Require(returned.Equals(expected) && actualLiteral.Equals(expected) && !retirement.IsCompleted);

                return true;
            });
            Require(runtime.Modules.DeleteModule(FallbackModule464).Equals(Term.A(ErlangBooleanAtoms.True)));
            await actor.InvokeAsync(async context =>
            {
                Require(await (ValueTask<bool>)Call(
                    runtime,
                    SourceLiteral819Names.Purge,
                    context,
                    FallbackModule464
                )!);
                object heap = Property(context, Heap);
                Call(
                    heap,
                    SourceLiteral819Names.Collect,
                    true,
                    (ushort)0
                );
                // Published code ownership is gone. Actual closure ENV in the
                // dictionary, not a CLR reference, still roots the literal area.
                Require(!retirement.IsCompleted && actualLiteral!.Equals(expected));
                Require(context.Dictionary.Remove(Term.A(FallbackKey464)));
                Call(
                    heap,
                    SourceLiteral819Names.Collect,
                    true,
                    (ushort)0
                );
                RequireStaleFunction464(literalOuter!);

                return true;
            });
            await retirement.WaitAsync(TimeSpan.FromSeconds(10));
            bool stale = false;
            try
            {
                _ = ((TupleTerm)actualLiteral!).Items[0];
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
        }
    }

    private static FunctionTerm StoredFunction464(ProcessContext context)
    {
        object dictionary = context.Dictionary;
        lock (Field(dictionary, SourceLiteral819Names.DictionaryGate))
            return (FunctionTerm)((Dictionary<Term, Term>)Field(dictionary, SourceLiteral819Names.DictionaryData))[Term.A(FallbackKey464)];
    }

    private static void RequireStaleFunction464(FunctionTerm function)
    {
        bool stale = false;
        try
        {
            _ = function.Arity;
        }
        catch (ObjectDisposedException)
        {
            stale = true;
        }
        Require(stale);
    }

    internal static void AssertLiteralChild(ProcessContext context, Term literal)
    {
        object heap = Property(context, Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int callbacks = 0;
        using var import = (IDisposable)Call(frame, PrepareImport293, Source([literal], () => callbacks++))!;
        var function = (FunctionTerm)Property(import, Value293);
        object block = Property(function, FunctionStorage293);
        // Actual literal child custody was captured at function construction.
        Flags(
            block,
            false,
            false,
            true
        );
        Call(
            frame,
            Set,
            (int)Call(frame, Reserve)!,
            function
        );
        lock (Property(heap, Gate293)) Require((bool)Call(
            heap,
            UseProof293,
            function,
            block
        )!);
        using var sidecar = (IDisposable?)Call(heap, LiteralSidecar293, function);
        Require(sidecar is not null);
        Term enclosing = (Term)Call(
            frame,
            ConsMethod,
            (int)Call(frame, Reserve)!,
            function,
            Nil.Value
        )!;
        Flags(
            StorageOf(heap, enclosing),
            false,
            false,
            true
        );
        using var parentSidecar = (IDisposable?)Call(heap, LiteralSidecar293, enclosing);
        Require(parentSidecar is not null && callbacks == 0);
    }

    private static FunctionTerm Source(Term[] environment, Action invoked)
        => new(
            new LocalFunctionIdentity(
                Term.A(IdentityModule293),
                null,
                0,
                0,
                new BitString(new byte[16])
            ),
            0,
            environment,
            (_, _) =>
            {
                invoked();
                throw new InvalidOperationException(Failure293);
            }
        );

    private static long Accumulate(
        ProcessRuntime runtime,
        object heap,
        object frame,
        FunctionTerm function,
        int count
    )
    {
        int slot = (int)Call(frame, Reserve)!;
        object before = Property(Property(runtime, Registry), Traversal);
        Term value = Nil.Value;
        for (int index = 0; index < count; index++)
            value = (Term)Call(
                frame,
                ConsMethod,
                slot,
                function,
                value
            )!;
        object after = Property(Property(runtime, Registry), Traversal);
        Flags(
            StorageOf(heap, value),
            true,
            true,
            true
        );
        Require((long)Property(after, Operations) - (long)Property(before, Operations) == count);
        long visits = (long)Property(after, Visits) - (long)Property(before, Visits);
        Call(frame, Clear, slot);

        return visits;
    }

    private static object StorageOf(object heap, Term value)
        => heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, [value])!;

    private static object Field(object owner, string name)
        => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static void Flags(
        object block,
        bool noBacking,
        bool noBinary,
        bool literalOnly
    )
    {
        Require((bool)Property(block, NoBacking) == noBacking
            && (bool)Property(block, NoBinary) == noBinary
            && (bool)Property(block, LiteralOnly293) == literalOnly
            && (bool)Property(block, Readonly293) == (noBacking || literalOnly));
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure293);
    }
}
