using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.DictionaryKeys538Names;

namespace Erlang.Tests;

internal static class DictionaryKeys538Checks
{
    internal static Task Native() => NativeMagicReference576Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            object payload = fixture.CreatePayload();
            var identity = (ReferenceTerm)Call(fixture.Runtime.ReferenceResources, DictionaryKeys605Names.Make, payload)!;
            actor = new RuntimeNodeFoundationHostActor(fixture.Runtime);
            await Exercise(
                fixture.Runtime,
                actor,
                identity,
                fixture.ReleaseNative,
                payload,
                DictionaryKeys605Names.Retired,
                true
            );
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        Finish(failures);
    });

    internal static async Task Atomics()
    {
        var fixture = new AtomicsP2a586Fixture();
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            actor = new RuntimeNodeFoundationHostActor(fixture.Runtime);
            await Exercise(
                fixture.Runtime,
                actor,
                fixture.Reference,
                () =>
 {
     fixture.ReleaseProducer();
     producer?.Dispose();
     producer = null;
 },
                fixture.Array,
                AtomicsP2a586Names.Retired,
                false
            );
            Require(!(bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                fixture.ReleaseInvocationForShutdown();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            await fixture.Drain(failures);
        }
        Finish(failures);
    }

    internal static async Task Counters()
    {
        var fixture = new CountersCustody596Fixture();
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(CountersCustody596Names.ProducerKind);
            actor = new RuntimeNodeFoundationHostActor(fixture.Runtime);
            await Exercise(
                fixture.Runtime,
                actor,
                fixture.Reference,
                () =>
 {
     fixture.ReleaseProducer();
     producer?.Dispose();
     producer = null;
 },
                fixture.Array,
                CountersCustody596Names.Retired,
                false
            );
            Require(!(bool)Property(fixture.Array, CountersCustody596Names.Allocated));
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                fixture.ReleaseInvocationForShutdown();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            await fixture.Drain(failures);
        }
        Finish(failures);
    }

    internal static Task Regex() => RegexCustody597Fixture.Run(async fixture =>
    {
        await Exercise(
            fixture.Runtime,
            fixture.Actor!,
            fixture.Reference,
            fixture.Release,
            fixture.Backing!,
            RegexCustody597Names.Retired,
            false
        );
        Require((bool)Property(fixture.Backing!, RegexCustody597Names.Freed));
    });

    internal static Task Binary() => BinaryArena598Fixture.Run(
        new BitString([120]),
        async fixture =>
    {
        await Exercise(
            fixture.Runtime,
            fixture.Actor!,
            fixture.Reference,
            fixture.Release,
            fixture.BackingOwner,
            BinaryArena598Names.Retired,
            false
        );
        Require((bool)Property(fixture.BackingOwner, BinaryArena598Names.Freed));
    }
    );

    private static async Task Exercise(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        ReferenceTerm identity,
        Action releaseProducer,
        object backing,
        string retiredName,
        bool native
    )
    {
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        using var compiled = GeneratedModuleCompiler.Compile(Source, DictionaryKeys538Names.File);
        compiled.Definition.Register(runtime.Modules);
        AssertCanonical(runtime.Modules, compiled.Definition);
        ProcessContext context = await actor.Ready.WaitAsync(Deadline);
        object heap = Property(context, DictionaryKeys605Names.Heap);
        IDisposable? sourceFrame = null;
        try
        {
            sourceFrame = (IDisposable)Call(heap, DictionaryKeys605Names.Enter)!;
            int slot = (int)Call(sourceFrame, DictionaryKeys605Names.Reserve)!;
            Call(
                sourceFrame,
                DictionaryKeys605Names.Set,
                slot,
                identity
            );
            var source = (ReferenceTerm)Call(sourceFrame, DictionaryKeys605Names.Read, slot)!;
            await actor.Invoke(owner =>
            {
                // Setup-only public transport; no Host value escapes this scope.
                using NativeTermHostScope524? setup = native ? new NativeTermHostScope524(runtime) : null;
                Require(owner.Dictionary.Count == 0);
                owner.Dictionary[source] = Term.I(Value);

                return true;
            });
            releaseProducer();
            Call(sourceFrame, DictionaryKeys605Names.Clear, slot);
            await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            bool stale = false;
            try
            {
                _ = source.Words;
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
            sourceFrame.Dispose();
            sourceFrame = null;
            Require(!((Task)Property(backing, retiredName)).IsCompleted);

            // No resource-bearing Term crosses to the Host. Both get_keys calls
            // happen before Source adds its held-selected-list dictionary entry.
            Term selected = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, DictionaryKeys538Names.Module, Select));
            Require(selected.Equals(Term.I(Words)));
            await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            Require(!((Task)Property(backing, retiredName)).IsCompleted);
            Term probe = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, DictionaryKeys538Names.Module, Probe));
            Require(probe.Equals(Term.I(Words)));
            await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            Require(!((Task)Property(backing, retiredName)).IsCompleted);
            Term dropped = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, DictionaryKeys538Names.Module, Drop));
            Require(dropped.Equals(Term.I(0)));
            // Drop's Source local frame has returned; only genuine heap GC can
            // release otherwise unreachable source/result headers now.
            await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            await ((Task)Property(backing, retiredName)).WaitAsync(Deadline);
            AssertCanonical(runtime.Modules, compiled.Definition);
            GC.KeepAlive(source);
            GC.KeepAlive(identity);
        }
        finally
        {
            // Never leave a temporary source frame across physical actor Done.
            sourceFrame?.Dispose();
        }
    }

    private static void AssertCanonical(ModuleRegistry registry, object sourceOwner)
    {
        foreach (string entry in new[] { Select, Probe, Drop })
            Require(ReferenceEquals(registry.SourceOwner(DictionaryKeys538Names.Module, entry, 0), sourceOwner));
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(Functions, Members)!.GetValue(registry)!;
        MethodInfo selected = typeof(ModuleRegistry).GetMethod(Selected, Members)!;
        foreach (int arity in new[] { 0, 1 })
        {
            var key = (ErlangModule, GetKeys, arity);
            Require(registry.SourceOwner(ErlangModule, GetKeys, arity) is null);
            object mode = selected.Invoke(registry, [key, functions[key]])!;
            Require(mode.ToString() == LocalDictionary);
        }
    }

    private static void Major(object heap)
        => Call(
            heap,
            DictionaryKeys605Names.Major,
            true,
            ushort.MaxValue
        );

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(failures);
    }
}
