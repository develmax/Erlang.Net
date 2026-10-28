using Erlang.Differential;
using System.Reflection;
using System.Linq.Expressions;
using Erlang.Compiler;
using static Erlang.Tests.BeamOwnedReceive507Names;

namespace Erlang.Tests;

internal static class BeamOwnedReceive507Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // Replays the existing, hash-verified original fixture and both original ETF
    // profiles unchanged. This is original parity; controls below are modeled.
    internal static async Task OriginalProtectedReceive()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();

        byte[] Read(string name) => (byte[])typeof(BeamProtectedReceiveHostChecks)
            .GetMethod(ReadResource, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [name])!;
        PreparedBeamImage image = runtime.Modules.PrepareBeam(
            Read(BeamProtectedReceiveHostNames.Beam),
            new BeamDependencySelection([ErlangModuleNames.Module]));
        Check(runtime.Modules.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        long maximum = 0;
        int receiveInstructions = 0;
        foreach (var row in new[]
        {
            (Entry: BeamProtectedReceiveHostNames.Run, Expected: BeamProtectedReceiveHostNames.Values),
            (Entry: BeamProtectedReceiveHostNames.RunStacks, Expected: BeamProtectedReceiveHostNames.Stacks)
        })
        {
            Term actual = await actor.InvokeAsync(async current =>
            {
                // The actor's queue predates the caller's AsyncLocal observer.
                using var inside = Checkpoint((owned, opcode) =>
                {
                    CollectNow(owned);
                    maximum = Math.Max(maximum, LiveWords(owned));
                    if (opcode == BeamCallbackPlan399Names.LoopReceiveOpcode)
                        receiveInstructions++;

                    return ValueTask.CompletedTask;
                });

                return await runtime.Modules.Call(current, image.Name, row.Entry);
            }).WaitAsync(Deadline);
            byte[] encoded = ExternalTermFormat.Encode(actual,
                new EtfEncodingOptions { FunctionOwner = runtime.InitializationPid });
            Check(encoded.AsSpan().SequenceEqual(Read(row.Expected)));
            Check(actual.Equals(runtime.ImportExternalTerm(Read(row.Expected)).Term));
        }
        Check(maximum > 0 && receiveInstructions > 0);
        await actor.DisposeAsync();
        await Physical(context);
        Empty(context);
    }

    internal static Task CancelBeforeCallback() => CancelAt(CallbackEntered);

    internal static Task CancelAfterSelection() => CancelAt(CallbackSelected);

    // An exact modeled call_fun receive plan exercises a selection that has
    // completed but is not yet admitted by the mailbox. No OTP golden claim.
    private static async Task CancelAt(string phase)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        BeamModule modeled = BeamModule.Prepare(BeamCallbackPlan399Bytes.Image());
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        var held = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var returned = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessContext? entered = null;
        var callback = new FunctionTerm(1, (_, arguments) => ValueTask.FromResult<Term>(Term.Tuple(Term.A(Chosen), arguments[0])));
        ProcessHandle child = runtime.Spawn(async context =>
        {
            entered = context;
            context.Mailbox.Send(Term.Tuple(Term.I(507), Term.List(Term.I(1), Term.I(2))));
            using var observation = Checkpoint(async (current, opcode) =>
            {
                CollectNow(current);
                if (opcode == phase)
                {
                    Check(LiveWords(current) > 0);
                    held.TrySetResult(current);
                    await release.Task;
                }
            });
            try
            {
                _ = await modeled.Invoke(
                    context,
                    GuestRun,
                    callback,
                    Term.Tuple(Term.I(17))
                );
                returned.TrySetResult(null);
            }
            catch (Exception error)
            {
                returned.TrySetResult(error);
                throw;
            }

            return Nil.Value;
        });
        try
        {
            ProcessContext context = await held.Task.WaitAsync(Deadline);
            await killer.Invoke(current =>
            {
                runtime.Exit(current, child.Pid, Term.A(Kill));

                return true;
            }).WaitAsync(Deadline);
            Check(!runtime.IsAlive(child.Pid));
            Check(!child.Completion.IsCompleted && !returned.Task.IsCompleted);
            CollectNow(context);
            Check(LiveWords(context) > 0);
            release.TrySetResult();
            Check(await returned.Task.WaitAsync(Deadline) is OperationCanceledException or ObjectDisposedException);
            _ = await child.Completion.WaitAsync(Deadline);
            await Physical(context);
            Empty(context);
        }
        finally
        {
            release.TrySetResult();
            if (runtime.IsAlive(child.Pid))
                await killer.Invoke(current =>
 {
     runtime.Exit(current, child.Pid, Term.A(Kill));

     return true;
 });
            _ = await child.Completion.WaitAsync(Deadline);
            if (entered is not null)
                await Physical(entered);
        }
    }

    internal static async Task MixedSourceSenderRetirement()
    {
        using var compiled = GeneratedModuleCompiler.Compile(OwnedGuest506Names.Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        compiled.Definition.Register(runtime.Modules);
        BeamModule modeled = BeamModule.Prepare(BeamCallbackPlan399Bytes.Image());
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext receiverContext = await receiver.Ready;
        ProcessContext? senderContext = null;
        Term payload = Term.Tuple(Term.I(507), Term.List(Term.I(1), Term.I(2)));
        ProcessHandle sender = runtime.Spawn(async current =>
        {
            senderContext = current;
            _ = await runtime.Modules.Call(
                current,
                OwnedGuest506Names.GuestModule,
                OwnedGuest506Names.SendEntry,
                receiver.Process.Pid,
                payload
            );

            return Nil.Value;
        });
        _ = await sender.Completion.WaitAsync(Deadline);
        await Physical(senderContext!);
        Empty(senderContext!);
        bool callbackSeen = false;
        var identity = new FunctionTerm(1, (_, arguments) =>
        {
            callbackSeen = true;

            return ValueTask.FromResult(arguments[0]);
        });
        Term result = await receiver.InvokeAsync(async current =>
        {
            using var observe = Checkpoint((owner, _) =>
 {
     CollectNow(owner);

     return ValueTask.CompletedTask;
 });

            return await modeled.Invoke(
                current,
                GuestRun,
                identity,
                Term.I(0)
            );
        }).WaitAsync(Deadline);
        Check(callbackSeen && result.Equals(Term.Tuple(Term.A(OwnedGuest506Names.MessageTag), payload)));
        await receiver.DisposeAsync();
        await Physical(receiverContext);
        Empty(receiverContext);
    }

    internal static async Task RemovedCandidateRollback()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Mailbox box = context.Mailbox;
            box.Send(Term.I(507));
            int calls = 0;
            object? result = await ReceiveModeled(
                box,
                ReceiveGuard,
                async candidate =>
            {
                calls++;
                var selection = (IDisposable)NewSelection(context);
                try
                {
                    CollectNow(context);
                    Check(LiveWords(context) > 0);
                    Check((await box.ReceiveAsync<Term>(item => item, TimeSpan.Zero))!.Equals(candidate));

                    return selection;
                }
                catch
                {
                    selection.Dispose();
                    throw;
                }
            }
            );
            Check(result is null && calls == 1 && box.Count == 0);
            CollectNow(context);
            Check(LiveWords(context) == 0);

            return true;
        }).WaitAsync(Deadline);
    }

    internal static async Task PendingAdmissionCancellation()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        var box = new Mailbox();
        box.Send(Term.I(507));
        using var cancellation = new CancellationTokenSource(Deadline);
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int waits = 0;
        typeof(Mailbox).GetProperty(WaitForExecution, Members)!.SetValue(
            box,
            (Func<ValueTask<long>>)(async () =>
        {
            if (Interlocked.Increment(ref waits) == 2)
            {
                held.TrySetResult();
                await release.Task;
            }

            return 0;
        })
        );
        object? claim = null;
        int released = 0;
        Type deferred = typeof(ProcessRuntime).Assembly.GetType(DeferredType, true)!;
        Delegate prepare = Expression.Lambda(typeof(Func<>).MakeGenericType(deferred),
            Expression.Constant(null, deferred)).Compile();
        Task<object?> operation = ReceiveModeled(
            box,
            ReceiveBeam,
            _ =>
        {
            object selection = NewSelection(context);
            try
            {
                Call(
                    box,
                    SendDeferred,
                    prepare,
                    (Action)(() => released++),
                    null,
                    null
                );
                claim = Call(box, ClaimDeferred);

                return ValueTask.FromResult<object?>(selection);
            }
            catch
            {
                ((IDisposable)selection).Dispose();
                throw;
            }
        },
            cancellation.Token
        ).AsTask();
        try
        {
            await held.Task.WaitAsync(Deadline);
            CollectNow(context);
            Check(LiveWords(context) > 0 && !operation.IsCompleted);
            cancellation.Cancel();
            Check(!operation.IsCompleted);
            CollectNow(context);
            Check(LiveWords(context) > 0);
            release.TrySetResult();
            try
            {
                _ = await operation.WaitAsync(Deadline);
                throw new InvalidOperationException(Failure);
            }
            catch (OperationCanceledException)
            {
            }
            CollectNow(context);
            Check(LiveWords(context) == 0);
        }
        finally
        {
            cancellation.Cancel();
            release.TrySetResult();
            try
            {
                _ = await operation.WaitAsync(Deadline);
            }
            catch (OperationCanceledException) { }
            if (claim is not null)
                ((Delegate)Get(claim, Complete)).DynamicInvoke([null]);
        }
        Check(released == 1);
        Check((await box.ReceiveAsync<Term>(item => item, TimeSpan.Zero))!.Equals(Term.I(507)));
        Check(box.Count == 0);
    }

    internal static async Task TraceFailureBothRoutes()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (bool saved in new[] { false, true })
            {
                var box = new Mailbox();
                var expected = new InvalidOperationException(Failure);
                typeof(Mailbox).GetProperty(MessageReceived, Members)!.SetValue(box, (Action)(() => throw expected));
                box.Send(Term.I(507));
                try
                {
                    _ = saved
                        ? await ReceiveModeled(box, ReceiveBeam, _ => ValueTask.FromResult<object?>(NewSelection(context)))
                        : await box.ReceiveAsync<object>(_ => NewSelection(context), TimeSpan.Zero);
                    throw new InvalidOperationException(Failure);
                }
                catch (Exception actual) when (ReferenceEquals(actual, expected))
                {
                }
                Check(box.Count == 0);
                CollectNow(context);
                Check(LiveWords(context) == 0);
            }

            return true;
        }).WaitAsync(Deadline);
    }

    private static async ValueTask<object?> ReceiveModeled(
        Mailbox box,
        string method,
        Func<Term, ValueTask<object?>> callback,
        CancellationToken cancellation = default
    )
    {
        MethodInfo entry = typeof(Mailbox).GetMethod(method, Members)!.MakeGenericMethod(typeof(object));

        return await (ValueTask<object?>)entry.Invoke(box, [callback, Term.I(0), cancellation])!;
    }

    private static object NewSelection(ProcessContext context)
    {
        var assembly = typeof(ProcessRuntime).Assembly;
        object state = Activator.CreateInstance(
            assembly.GetType(StateType, true)!,
            Members,
            null,
            [null, context],
            null
        )!;
        try
        {
            Call(
                state,
                Write,
                0,
                Term.Tuple(Term.I(507), Term.List(Term.I(1), Term.I(2)))
            );
            Term value = (Term)Call(state, Read, 0)!;

            return Activator.CreateInstance(
                assembly.GetType(SelectionType, true)!,
                Members,
                null,
                [0, state, new Term[] { value }],
                null
            )!;
        }
        catch
        {
            ((IDisposable)state).Dispose();
            throw;
        }
    }

    internal static async Task MetadataRootOnly()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            var assembly = typeof(ProcessRuntime).Assembly;
            object state = Activator.CreateInstance(
                assembly.GetType(StateType, true)!,
                Members,
                null,
                [null, context],
                null
            )!;
            IDisposable? selection = null;
            try
            {
                Call(
                    state,
                    Write,
                    0,
                    Term.Tuple(Term.I(507))
                );
                Term marker = (Term)Call(state, Read, 0)!;
                selection = (IDisposable)Activator.CreateInstance(
                    assembly.GetType(SelectionType, true)!,
                    Members,
                    null,
                    [0, state, new Term[] { marker }],
                    null
                )!;
                Call(state, Clear, Array.Empty<int>());
                CollectNow(context);
                Check(LiveWords(context) > 0);
                Check(((TupleTerm)marker).Items[0].Equals(Term.I(507)));
                selection.Dispose();
                selection = null;
                CollectNow(context);
                Check(LiveWords(context) == 0);
            }
            finally
            {
                selection?.Dispose();
                ((IDisposable)state).Dispose();
            }

            return true;
        }).WaitAsync(Deadline);
    }

    private static object? Call(object instance, string name, params object?[] arguments)
        => instance.GetType().GetMethods(Members).Single(method => method.Name == name
            && method.GetParameters().Length == arguments.Length
            && method.GetParameters().Select((parameter, index) => arguments[index] is null
                || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(value => value))
            .Invoke(instance, arguments);

    private static IDisposable Checkpoint(Func<ProcessContext, string, ValueTask> callback)
        => (IDisposable)typeof(ProcessRuntime).Assembly.GetType(CheckpointType, true)!
            .GetMethod(Enter, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [callback])!;

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static void CollectNow(ProcessContext context)
    {
        object heap = Get(context, Heap);
        _ = heap.GetType().GetMethod(Collect, Members)!.Invoke(heap, null);
    }

    private static long LiveWords(ProcessContext context) => (long)Get(Get(Get(context, Heap), Snapshot), Words);

    private static async Task Physical(ProcessContext context)
        => await ((Task)Get(Get(context, Unwound), TaskProperty)).WaitAsync(Deadline);

    private static void Empty(ProcessContext context)
    {
        object heap = Get(Get(context, Heap), Snapshot);
        object native = Get(Get(context, NativeArena), Snapshot);
        Check((long)Get(heap, Words) == 0 && (long)Get(heap, Capacity) == 0);
        Check((long)Get(native, NativeReserved) == 0 && (long)Get(native, NativeRooted) == 0);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
