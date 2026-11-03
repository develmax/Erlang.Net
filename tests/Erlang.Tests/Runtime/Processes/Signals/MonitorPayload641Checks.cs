using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MonitorPayload641Names;

namespace Erlang.Tests;

internal static class MonitorPayload641Checks
{
    internal static Task Run(MonitorPayload641Mode mode, string family) => family switch
    {
        MonitorPayload641Names.NativeFamily => Native(mode),
        MonitorPayload641Names.AtomicsFamily => Atomics(mode),
        MonitorPayload641Names.CountersFamily => Counters(mode),
        MonitorPayload641Names.RegexFamily => Regex(mode),
        MonitorPayload641Names.BinaryFamily => Binary(mode),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    private static Task Native(MonitorPayload641Mode mode) => NativeMagicReference576Fixture.Run(async fixture =>
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

    private static async Task Atomics(MonitorPayload641Mode mode)
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

    private static async Task Counters(MonitorPayload641Mode mode)
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

    private static Task Regex(MonitorPayload641Mode mode) => RegexCustody597Fixture.Run(fixture => Exercise(
        fixture.Runtime,
        fixture.Actor!,
        fixture.Reference,
        fixture.Release,
        fixture.Backing!,
        RegexCustody597Names.Retired,
        mode,

        false
    ));

    private static Task Binary(MonitorPayload641Mode mode) => BinaryArena598Fixture.Run(
        new BitString([120]),
        fixture => Exercise(
            fixture.Runtime,
            fixture.Actor!,
            fixture.Reference,
            fixture.Release,
            fixture.BackingOwner,
            BinaryArena598Names.Retired,
            mode,

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
        MonitorPayload641Mode mode,
        bool nativeScope,
        Action? releaseInvocationForShutdown = null
    )
    {
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        using PreparedBeamImage counters = CountersOriginal421Preparation.Prepare(runtime, runtime.Modules);
        await runtime.InitializeModulesAsync(registry =>
        {
            Require(registry.PublishPreparedBeam(counters).Status == BeamPublicationStatus.Published);
            CountersOriginal421Preparation.AssertPublished(registry, counters);
        });
        using var compiled = GeneratedModuleCompiler.Compile(Source, MonitorPayload641Names.File);
        compiled.Definition.Register(runtime.Modules);
        ProcessContext context = await actor.Ready.WaitAsync(Deadline);
        object heap = Property(context, NativeMagicReference576Names.Heap);
        ReferenceTerm? staleSource = null;
        await actor.Invoke(owner =>
        {
            using NativeTermHostScope524? host = nativeScope ? new(runtime) : null;
            owner.Dictionary[Term.A(Stored)] = reference;
            var data = (Dictionary<Term, Term>)typeof(ProcessDictionary)
                .GetField(DictionaryData, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner.Dictionary)!;
            staleSource = (ReferenceTerm)data[Term.A(Stored)];

            return true;
        });
        releaseProducer();
        // This is the actual generated Source body and canonical monitor/3;
        // no manual OwnedInvocation or public callback is promoted to Source.
        _ = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, MonitorPayload641Names.Module, Arm));
        await actor.Invoke(_ =>
        {
            Major(heap);

            return true;
        });
        bool stale = false;
        try
        {
            _ = staleSource!.Words;
        }
        catch (ObjectDisposedException)
        {
            stale = true;
        }
        Require(stale && !((Task)Property(backing, retiredName)).IsCompleted);
        if (mode == MonitorPayload641Mode.Down)
        {
            Term value = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, MonitorPayload641Names.Module, Take));
            Require(value.Equals(Term.A(ErlangBooleanAtoms.True)));
            await actor.Invoke(_ =>
            {
                Major(heap);

                return true;
            });
        }
        else if (mode == MonitorPayload641Mode.Cancel)
        {
            Term value = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, MonitorPayload641Names.Module, Cancel));
            Require(value.Equals(Term.A(ErlangBooleanAtoms.True)));
            await actor.Invoke(_ =>
            {
                Major(heap);

                return true;
            });
        }
        else if (mode == MonitorPayload641Mode.OwnerExit)
            await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
        else
        {
            releaseInvocationForShutdown?.Invoke();
            await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
        }
        await ((Task)Property(backing, retiredName)).WaitAsync(Deadline);
        GC.KeepAlive(staleSource);
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
