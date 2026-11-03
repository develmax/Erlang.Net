using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.TimerCallback539Names;

namespace Erlang.Tests;

internal static class TimerCallback539Checks
{
    internal static Task FireClaimBeforeCancel() => Run(false);

    internal static Task VmCloseDuringPublication() => Run(true);

    private static Task Run(bool close) => NativeMagicReference576Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        object? phase = null;
        Task? shutdown = null;
        try
        {
            ProcessRuntime runtime = fixture.Runtime;
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
            phase = Activator.CreateInstance(typeof(ProcessRuntime).Assembly.GetType(PhaseType, true)!, true)!;
            // Attach before ANY timer admission. No reflection call to Fire.
            Call(Property(runtime, Timers), Attach, phase);
            object payload = fixture.CreatePayload();
            var reference = (ReferenceTerm)Call(runtime.ReferenceResources, DictionaryKeys605Names.Make, payload)!;
            actor = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            object heap = Property(context, DictionaryKeys605Names.Heap);
            ReferenceTerm? source = null;
            await actor.Invoke(owner =>
            {
                using var scope = new NativeTermHostScope524(runtime);
                owner.Dictionary[Term.A(Stored)] = reference;
                var data = (Dictionary<Term, Term>)typeof(ProcessDictionary)
                    .GetField(DictionaryData, Members)!.GetValue(owner.Dictionary)!;
                source = (ReferenceTerm)data[Term.A(Stored)];

                return true;
            });
            fixture.ReleaseNative();
            using var compiled = GeneratedModuleCompiler.Compile(Source, TimerCallback539Names.File);
            compiled.Definition.Register(runtime.Modules);
            Term timer = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                owner,
                TimerCallback539Names.Module,
                Arm,
                Term.I(close ? 1 : 0)
            ));
            await ((Task)Property(phase, Claimed)).WaitAsync(Deadline);
            Require((int)Property(phase, Claims) == 1);
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
            Require(stale && !((Task)Property(payload, DictionaryKeys605Names.Retired)).IsCompleted);
            if (close)
            {
                // The real callback is blocked OUTSIDE R with publication interest. A
                // correct Dispose must await its Timer.DisposeAsync/payload owner.
                shutdown = runtime.DisposeAsync().AsTask();
                Require(!shutdown.IsCompleted);
                Require(!((Task)Property(payload, DictionaryKeys605Names.Retired)).IsCompleted);
                Call(phase, Release);
                await shutdown.WaitAsync(Deadline);
                await ((Task)Property(phase, Exited)).WaitAsync(Deadline);
            }
            else
            {
                Term cancelled = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                    owner,
                    ErlangModule,
                    CancelTimer,
                    timer
                ));
                Require(cancelled.Equals(Term.A(False)));
                Require(!((Task)Property(payload, DictionaryKeys605Names.Retired)).IsCompleted);
                Call(phase, Release);
                await ((Task)Property(phase, Exited)).WaitAsync(Deadline);
                Term value = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                    owner,
                    TimerCallback539Names.Module,
                    Take,
                    timer
                ));
                Require(value.Equals(Term.I(DictionaryKeys605Names.NativeWords)));
                Term empty = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                    owner, TimerCallback539Names.Module, TimerCallback539Names.Empty
                ));
                Require(empty.Equals(Term.A(EmptyAtom)));
                await actor.Invoke(_ =>
 {
     Major(heap);

     return true;
 });
            }
            await ((Task)Property(payload, DictionaryKeys605Names.Retired)).WaitAsync(Deadline);
            Require((int)Property(phase, Claims) == 1);
            GC.KeepAlive(source);
            GC.KeepAlive(reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // FIRST release the actual blocked callback, even after a failed
            // assertion. Never wait actor/VM/retirement with the gate still held.
            if (phase is not null)
                try
                {
                    Call(phase, Release);
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
            try
            {
                if (shutdown is not null) await shutdown.WaitAsync(Deadline);
                else await fixture.Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (phase is not null)
            {
                try
                {
                    Task claimed = (Task)Property(phase, Claimed);
                    if (claimed.IsCompleted) await ((Task)Property(phase, Exited)).WaitAsync(Deadline);
                    ((IDisposable)phase).Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
        }
        Finish(failures);
    });

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
