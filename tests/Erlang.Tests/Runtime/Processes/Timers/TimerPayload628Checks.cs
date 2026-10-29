using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.TimerPayload628Names;

namespace Erlang.Tests;

internal static class TimerPayload628Checks
{
    internal static Task Run(TimerPayload628Mode mode, string family) => family switch
    {
        TimerPayload628Names.NativeFamily => Native(mode),
        TimerPayload628Names.AtomicsFamily => Atomics(mode),
        TimerPayload628Names.CountersFamily => Counters(mode),
        TimerPayload628Names.RegexFamily => Regex(mode),
        TimerPayload628Names.BinaryFamily => Binary(mode),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    internal static Task OpaqueAlias() => Native(TimerPayload628Mode.OpaqueAlias);

    private static Task Native(TimerPayload628Mode mode) => NativeMagicReference576Fixture.Run(async fixture =>
    {
        var errors = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            object payload = fixture.CreatePayload();
            var reference = (ReferenceTerm)Call(fixture.Runtime.ReferenceResources, NativeMakeReference, payload)!;
            actor = new(fixture.Runtime);
            await Exercise(
                fixture.Runtime,
                actor,
                reference,
                fixture.ReleaseNative,
                payload,
                NativeRetired,
                mode,
                SendAfterThree,
                true
            );
        }
        catch (Exception error)
        {
            errors.Add(error);
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
                    errors.Add(error);
                }
        }
        Finish(errors);
    });

    private static async Task Atomics(TimerPayload628Mode mode)
    {
        var fixture = new AtomicsP2a586Fixture();
        var errors = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            actor = new(fixture.Runtime);
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
                mode,
                StartTimerThree,
                false,
                fixture.ReleaseInvocationForShutdown
            );
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            await fixture.Drain(errors);
        }
        Finish(errors);
    }

    private static async Task Counters(TimerPayload628Mode mode)
    {
        var fixture = new CountersCustody596Fixture();
        var errors = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(CountersCustody596Names.ProducerKind);
            actor = new(fixture.Runtime);
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
                mode,
                SendAfterThree,
                false,
                fixture.ReleaseInvocationForShutdown
            );
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            await fixture.Drain(errors);
        }
        Finish(errors);
    }

    private static Task Regex(TimerPayload628Mode mode) => RegexCustody597Fixture.Run(fixture => Exercise(
        fixture.Runtime,
        fixture.Actor!,
        fixture.Reference,
        fixture.Release,
        fixture.Backing!,
        RegexCustody597Names.Retired,
        mode,
        SendAfterFour,
        false
    ));

    private static Task Binary(TimerPayload628Mode mode) => BinaryArena598Fixture.Run(
        new BitString([120]),
        fixture => Exercise(
            fixture.Runtime,
            fixture.Actor!,
            fixture.Reference,
            fixture.Release,
            fixture.BackingOwner,
            BinaryArena598Names.Retired,
            mode,
            StartTimerFour,
            false
        )
    );

    private static async Task Exercise(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        ReferenceTerm reference,
        Action releaseProducer,
        object backing,
        string retiredName,
        TimerPayload628Mode mode,
        int operation,
        bool nativeScope,
        Action? releaseInvocationForShutdown = null
    )
    {
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        using var compiled = GeneratedModuleCompiler.Compile(Source, TimerPayload628Names.File);
        compiled.Definition.Register(runtime.Modules);
        ProcessContext context = await actor.Ready.WaitAsync(Deadline);
        object heap = Property(context, NativeMagicReference576Names.Heap);
        ReferenceTerm? source = null;
        await actor.Invoke(owner =>
        {
            using NativeTermHostScope524? host = nativeScope ? new(runtime) : null;
            owner.Dictionary[Term.A(Stored)] = reference;
            // Observe the actual permanent-frame wrapper without creating a
            // public Host copy. Strong diagnostic CLR references are not roots.
            var data = (Dictionary<Term, Term>)typeof(ProcessDictionary)
                .GetField(DictionaryData, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner.Dictionary)!;
            source = (ReferenceTerm)data[Term.A(Stored)];

            return true;
        });
        releaseProducer();
        if (mode == TimerPayload628Mode.OpaqueAlias)
        {
            var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
                .GetField(Functions, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
            ErlangFunction actual = functions[(TimerPayload628Names.Erlang, SendAfter, 3)];
            runtime.Modules.Register(
                PublicAlias,
                SendAfter,
                3,
                actual
            );
            Require(runtime.Modules.SourceOwner(PublicAlias, SendAfter, 3) is null);
            bool refused = false;
            try
            {
                await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, TimerPayload628Names.Module, Opaque));
            }
            catch (NotSupportedException error) when (error.Message == NativeGuest535Names.ScopeRequired)
            {
                refused = true;
            }
            Require(refused);
            await actor.Invoke(owner =>
 {
     owner.Dictionary.Clear();
     Major(heap);

     return true;
 });
            await ((Task)Property(backing, retiredName)).WaitAsync(Deadline);
            GC.KeepAlive(source);

            return;
        }
        Term timer = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
            owner,
            TimerPayload628Names.Module,
            Arm,
            Term.I(mode == TimerPayload628Mode.Fire ? FireDelay : PendingDelay),
            Term.I(operation)
        ));
        var failures = new List<Exception>();
        try
        {
            await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            bool stale = false;
            try
            {
                _ = source!.Words;
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale && !((Task)Property(backing, retiredName)).IsCompleted);
            if (mode == TimerPayload628Mode.Fire)
            {
                Term value = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, TimerPayload628Names.Module, Take));
                Require(value.Equals(Term.I(MagicWords)));
                await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            }
            else if (mode == TimerPayload628Mode.Cancel)
            {
                Term remaining = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                    owner,
                    TimerPayload628Names.Erlang,
                    TimerPayload628Names.CancelTimer,
                    timer
                ));
                Require(remaining is Integer integer && integer.Value >= 0);
            }
            else if (mode == TimerPayload628Mode.OwnerExit)
                await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
            else
            {
                releaseInvocationForShutdown?.Invoke();
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (mode == TimerPayload628Mode.Cancel)
            {
                try
                {
                    await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                        owner,
                        TimerPayload628Names.Erlang,
                        TimerPayload628Names.CancelTimer,
                        timer
                    ));
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
        }
        if (failures.Count == 0)
            await ((Task)Property(backing, retiredName)).WaitAsync(Deadline);
        Finish(failures);
        GC.KeepAlive(source);
        GC.KeepAlive(reference);
    }

    private static void Major(object heap)
        => Call(
            heap,
            MajorCollect,
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
        if (failures.Count > 1) throw new AggregateException(Failure, failures);
    }
}
