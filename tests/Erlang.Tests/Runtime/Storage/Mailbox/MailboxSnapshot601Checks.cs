using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxSnapshot601TestNames;

namespace Erlang.Tests;

internal static class MailboxSnapshot601Checks
{
    internal static Task PublicSnapshot() => MailboxSnapshot601Fixture.Run(async fixture =>
    {
        IDisposable? captured = null;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failures = new List<Exception>();
        Task holder = Task.Run(async () =>
        {
            try
            {
                captured = (IDisposable)Call(fixture.MailboxOwner, CaptureSnapshot)!;
                ready.TrySetResult();
                await release.Task.WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                ready.TrySetException(error);
                throw;
            }
        });
        try
        {
            await ready.Task.WaitAsync(Deadline);
            await fixture.Dequeue();
            MailboxSnapshot601Fixture.Require(!((Task)Property(fixture.Payload, Retired)).IsCompleted);
            using (var host = new NativeTermHostScope524(fixture.Runtime))
            {
                var values = (IReadOnlyList<Term>)Call(fixture.MailboxOwner, CompleteSnapshot, captured)!;
                MailboxSnapshot601Fixture.Require(values.Count == 1);
                fixture.Adopt(values[0]);
            }
            (captured ?? throw new InvalidOperationException(Failure)).Dispose();
            captured = null;
            release.TrySetResult();
            await holder.WaitAsync(Deadline);
            await fixture.VerifyAndRetire();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await holder.WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                captured?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        Finish(failures);
    });

    internal static Task SourceSelfInfo() => MailboxSnapshot601Fixture.Run(async fixture =>
    {
        using var compiled = GeneratedModuleCompiler.Compile(Source, GuestModule + ".erl");
        compiled.Definition.Register(fixture.Runtime.Modules);
        Term result = await fixture.Target.InvokeAsync(async context =>
        {
            Term scalar = await fixture.Runtime.Modules.Call(context, GuestModule, SelfEntry);
            Console.WriteLine(SourceScalarObservation, SelfEntry, scalar);
            Call(
                Property(context, Heap),
                Major,
                true,
                ushort.MaxValue
            );
            Console.WriteLine(SourceCollectedObservation, SelfEntry);

            return scalar;
        });
        MailboxSnapshot601Fixture.Require(result.Equals(Term.I(NativeHeaderWords)));
        await ((Task)Property(fixture.Payload, Retired)).WaitAsync(Deadline);
    });

    internal static Task SourceRemoteInfo() => SourceRemote(false);

    internal static Task PublicReregister() => SourceRemote(true);

    private static Task SourceRemote(bool publicOverride) => MailboxSnapshot601Fixture.Run(async fixture =>
    {
        using var compiled = GeneratedModuleCompiler.Compile(Source, GuestModule + ".erl");
        compiled.Definition.Register(fixture.Runtime.Modules);
        if (publicOverride)
        {
            var bodies = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
                .GetField(Functions, Members)!.GetValue(fixture.Runtime.Modules)!;
            ErlangFunction original = bodies[(ErlangModuleNames.Module, ProcessInfoNames.Function, 2)];
            // Existing public API requires module deletion before republication.
            // This isolated runtime keeps the exact body but must lose trust.
            fixture.Runtime.Modules.DeleteModule(ErlangModuleNames.Module);
            fixture.Runtime.Modules.Register(
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                2,
                original
            );
        }
        fixture.BeginPump();
        fixture.Runtime.Modules.Register(
            Probe,
            After,
            1,
            async (context, arguments) =>
        {
            MailboxSnapshot601Fixture.Require(arguments[0].Equals(fixture.TargetContext.Self));
            await fixture.Dequeue();
            // R is no longer in the target queue or a producer/Host scope.
            // Only genuine compiled source/result roots may keep it live here.
            Call(
                Property(context, Heap),
                Major,
                true,
                ushort.MaxValue
            );
            MailboxSnapshot601Fixture.Require(!((Task)Property(fixture.Payload, Retired)).IsCompleted);

            return Nil.Value;
        }
        );
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? requester = null;
        try
        {
            requester = new(fixture.Runtime);
            await requester.Ready.WaitAsync(Deadline);
            await requester.InvokeAsync(async context =>
            {
                // Scalar result: deliberately NO Host scope may mask missing
                // genuine compiled caller ownership of the process_info value.
                try
                {
                    Term result = await fixture.Runtime.Modules.Call(
                        context,
                        GuestModule,
                        Entry,
                        fixture.TargetContext.Self
                    );
                    MailboxSnapshot601Fixture.Require(!publicOverride && result.Equals(Term.I(NativeHeaderWords)));
                    Console.WriteLine(SourceScalarObservation, Entry, result);
                    Call(
                        Property(context, Heap),
                        Major,
                        true,
                        ushort.MaxValue
                    );
                    Console.WriteLine(SourceCollectedObservation, Entry);
                }
                catch (NotSupportedException error) when (publicOverride && error.Message == HostScopeRequired)
                {
                    return Nil.Value;
                }

                return Nil.Value;
            });
            if (publicOverride) await fixture.Dequeue();
            await ((Task)Property(fixture.Payload, Retired)).WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (requester is not null)
                try
                {
                    await requester.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        Finish(failures);
    });

    internal static async Task DefaultProfile()
    {
        var failures = new List<Exception>();
        var runtime = new ProcessRuntime();
        RuntimeNodeFoundationHostActor? target = null, requester = null;
        Task<Term>? pump = null;
        ProcessContext? targetContext = null;
        try
        {
            target = new(runtime);
            requester = new(runtime);
            targetContext = await target.Ready.WaitAsync(Deadline);
            ProcessContext caller = await requester.Ready.WaitAsync(Deadline);
            FieldInfo heap = typeof(ProcessContext).GetField(HeapField, Members)!;
            object callerBirthStorage = heap.GetValue(caller)!;
            object targetBirthStorage = heap.GetValue(targetContext)!;
            MailboxSnapshot601Fixture.Require(callerBirthStorage is not null && targetBirthStorage is not null
                && !ReferenceEquals(callerBirthStorage, targetBirthStorage)
                && !(bool)typeof(ProcessContext).GetProperty("OwnedExecutionEnabled683", Members)!.GetValue(caller)!
                && !(bool)typeof(ProcessContext).GetProperty("OwnedExecutionEnabled683", Members)!.GetValue(targetContext)!);
            runtime.Send(targetContext.Self, Term.I(DefaultMessage));
            // The queued message now owns a real frame. The private info request
            // must leave the actual already-queued heap/root state unchanged.
            object callerBirthSnapshot = callerBirthStorage!.GetType().GetProperty("Snapshot", Members)!.GetValue(callerBirthStorage)!;
            object targetBirthSnapshot = targetBirthStorage!.GetType().GetProperty("Snapshot", Members)!.GetValue(targetBirthStorage)!;
            pump = target.InvokeAsync(async context =>
            {
                await context.ReceiveAsync<Term>(value => value is Atom atom && atom.Name == Stop ? value : null, Deadline);

                return (Term)Nil.Value;
            });
            Term result = await requester.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    targetContext.Self,
                    Term.A(Messages)
                ));
            MailboxSnapshot601Fixture.Require(result.Equals(Term.Tuple(Term.A(Messages), Term.List(Term.I(DefaultMessage)))));
            MailboxSnapshot601Fixture.Require(ReferenceEquals(heap.GetValue(caller), callerBirthStorage)
                && ReferenceEquals(heap.GetValue(targetContext), targetBirthStorage)
                && callerBirthSnapshot.Equals(callerBirthStorage!.GetType().GetProperty("Snapshot", Members)!.GetValue(callerBirthStorage))
                && targetBirthSnapshot.Equals(targetBirthStorage!.GetType().GetProperty("Snapshot", Members)!.GetValue(targetBirthStorage))
                && !(bool)typeof(ProcessContext).GetProperty("OwnedExecutionEnabled683", Members)!.GetValue(caller)!
                && !(bool)typeof(ProcessContext).GetProperty("OwnedExecutionEnabled683", Members)!.GetValue(targetContext)!);
            runtime.Send(targetContext.Self, Term.A(Stop));
            await pump.WaitAsync(Deadline);
            pump = null;
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (pump is not null)
            {
                try
                {
                    if (targetContext is not null) runtime.Send(targetContext.Self, Term.A(Stop));
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                try
                {
                    await pump.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
            if (requester is not null)
                try
                {
                    await requester.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (target is not null)
                try
                {
                    await target.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        Finish(failures);
    }

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }
}
