using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.ReasonInput645Names;

namespace Erlang.Tests;

internal static class ReasonInput645Checks
{
    internal static Task Run(ReasonInput645Mode mode, string family) => family switch
    {
        ReasonInput645Names.NativeFamily => Native(mode),
        ReasonInput645Names.AtomicsFamily => Atomics(mode),
        ReasonInput645Names.CountersFamily => Counters(mode),
        ReasonInput645Names.RegexFamily => Regex(mode),
        ReasonInput645Names.BinaryFamily => Binary(mode),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    private static Task Native(ReasonInput645Mode mode) => NativeMagicReference576Fixture.Run(async fixture =>
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

    private static async Task Atomics(ReasonInput645Mode mode)
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

    private static async Task Counters(ReasonInput645Mode mode)
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

    private static Task Regex(ReasonInput645Mode mode) => RegexCustody597Fixture.Run(fixture => Exercise(
        fixture.Runtime,
        fixture.Actor!,
        fixture.Reference,
        fixture.Release,
        fixture.Backing!,
        RegexCustody597Names.Retired,
        mode,

        false
    ));

    private static Task Binary(ReasonInput645Mode mode) => BinaryArena598Fixture.Run(
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
        ReasonInput645Mode mode,
        bool nativeScope,
        Action? releaseInvocationForShutdown = null
    )
    {
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        using var compiled = GeneratedModuleCompiler.Compile(Source, ReasonInput645Names.FileName);
        compiled.Definition.Register(runtime.Modules);
        Require(ReferenceEquals(runtime.Modules.SourceOwner(ReasonInput645Names.Module, Arm, 1), compiled.Definition));
        Require(ReferenceEquals(runtime.Modules.SourceOwner(ReasonInput645Names.Module, Take, 0), compiled.Definition));
        Require(ReferenceEquals(runtime.Modules.SourceOwner(ReasonInput645Names.Module, Drop, 0), compiled.Definition));
        ProcessContext context = await actor.Ready.WaitAsync(Deadline);
        object heap = Property(context, NativeMagicReference576Names.Heap);
        ReferenceTerm? staleSource = null;
        ProcessHandle? child = null;
        var failures = new List<Exception>();
        try
        {
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
            var childPid = (Pid)await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                owner,
                ReasonInput645Names.Module,
                Arm,
                Term.A(mode == ReasonInput645Mode.Closure ? Closure : Mfa)
            ));
            child = runtime.GetProcessHandle(childPid);
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
            Term value = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, ReasonInput645Names.Module, Take));
            Require(value.Equals(Term.A(ErlangBooleanAtoms.True)));
            // Actual child unwind precedes last receiver-root release. No resource
            // is read or re-admitted from the opaque Completion value.
            _ = await child.Completion.WaitAsync(Deadline);
            await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            Require(!((Task)Property(backing, retiredName)).IsCompleted);
            value = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, ReasonInput645Names.Module, Drop));
            Require(value.Equals(Term.A(ErlangBooleanAtoms.True)));
            await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            await ((Task)Property(backing, retiredName)).WaitAsync(Deadline);
            Require(ReferenceEquals(runtime.Modules.SourceOwner(ReasonInput645Names.Module, Take, 0), compiled.Definition));
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (child is not null)
                try
                {
                    await actor.Invoke(owner =>
 {
     runtime.Exit(owner, child.Pid, Term.A(ProcessExitSignals.Kill));

     return true;
 });
                    _ = await child.Completion.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                _ = await actor.InvokeAsync(async owner => await runtime.Modules.Call(owner, ReasonInput645Names.Module, Drop));
                await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            GC.KeepAlive(staleSource);
            GC.KeepAlive(reference);
        }
        Finish(failures);
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
