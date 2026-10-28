using System.Reflection;
using Erlang.Differential;
using Erlang.Compiler;
using static Erlang.Tests.OwnedGuest506Names;

namespace Erlang.Tests;

internal static class OwnedGuest506Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static async Task Payload()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        bool observed = false;
        runtime.Modules.Register(
            ProbeModule,
            Live,
            1,
            (context, arguments) =>
        {
            // Registered C# bodies receive detached copies, not borrowed cells.
            RequirePortable(arguments[0]);
            object heap = Get(context, Heap);
            Check((long)Get(Get(heap, Snapshot), Words) > 0);
            Call(heap, Collect);
            observed = true;

            return ValueTask.FromResult(arguments[0]);
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term expected = Term.Tuple(Term.I(506), Term.List(Term.I(1), Term.I(2)));
        Term result = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Run,
            Term.I(506)
        ));
        Check(observed && result.Equals(expected));
        RequirePortable(result);
        await actor.Invoke(context =>
        {
            object heap = Get(context, Heap);
            Call(heap, Collect);
            Check((long)Get(Get(heap, Snapshot), Words) > 0);
            Check(context.Dictionary[Term.A(Key)].Equals(expected));
            context.Dictionary.Clear();
            Call(heap, Collect);
            Check((long)Get(Get(heap, Snapshot), Words) == 0);
            // The host value remains valid after all guest cells were reclaimed.
            Check(result.Equals(expected));

            return true;
        });
    }

    internal static async Task AwaitAndClosure()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        FunctionTerm function = (FunctionTerm)await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                GuestModule,
                Make,
                Term.I(506)
            ));
        foreach (Term capture in function.Environment)
            RequirePortable(capture);
        await actor.Invoke(context =>
 {
     Call(Get(context, Heap), Collect);

     return true;
 });
        Term invoked = await actor.InvokeAsync(async context => await function.Invoke(context, []));
        Check(invoked.Equals(Term.Tuple(Term.I(506), Term.List(Term.I(1), Term.I(2)))));
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            ProbeModule,
            Held,
            1,
            async (context, arguments) =>
        {
            RequirePortable(arguments[0]);
            Check((long)Get(Get(Get(context, Heap), Snapshot), Words) > 0);
            entered.TrySetResult(context);
            await System.Threading.Tasks.Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);

            return arguments[0];
        }
        );
        Task<Term> pending = actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Held,
            Term.I(507)
        ));
        ProcessContext held = await entered.Task.WaitAsync(Deadline);
        await killer.Invoke(context =>
 {
     runtime.Exit(context, actor.Process.Pid, Term.A(Kill));

     return true;
 });
        try
        {
            await pending.WaitAsync(Deadline);
            throw new InvalidOperationException(Failure);
        }
        catch (OperationCanceledException) { }
        await ((Task)Get(Get(held, Unwound), TaskProperty)).WaitAsync(Deadline);
        Check((long)Get(Get(Get(held, Heap), Snapshot), Words) == 0);
        Check((long)Get(Get(Get(held, Heap), Snapshot), Capacity) == 0);
    }

    internal static async Task CapturedTask()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        runtime.Modules.Register(
            ProbeModule,
            Live,
            1,
            (_, arguments) => ValueTask.FromResult(arguments[0])
        );
        runtime.Modules.Register(
            ProbeModule,
            Echo,
            1,
            (_, arguments) => ValueTask.FromResult(arguments[0])
        );
        FunctionTerm closure = (FunctionTerm)await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                GuestModule,
                Make,
                Term.I(506)
            ));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Term[]>? captured = null;
        runtime.Modules.Register(
            ProbeModule,
            Capture,
            1,
            (context, arguments) =>
        {
            RequirePortable(arguments[0]);
            captured = System.Threading.Tasks.Task.Run(async () =>
            {
                // Deliberately inherit the callback's ExecutionContext, then wait
                // until all original caller/callback root scopes have disposed.
                await release.Task;
                Term registry = await runtime.Modules.Call(
                    context,
                    ProbeModule,
                    Echo,
                    arguments[0]
                );
                Term compiler = await Execution.InvokeAsync(
                    artifact.Definition,
                    Run,
                    context,
                    [Term.I(506)]
                );
                Term function = await closure.Invoke(context, []);

                return new[] { registry, compiler, function };
            });

            return ValueTask.FromResult(arguments[0]);
        }
        );
        try
        {
            Term original = await actor.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    GuestModule,
                    Capture,
                    Term.I(506)
                ));
            RequirePortable(original);
            Check(captured is not null && !captured.IsCompleted);
            release.TrySetResult();
            Term[] results = await captured!.WaitAsync(Deadline);
            foreach (Term result in results)
            {
                Check(result.Equals(original));
                RequirePortable(result);
            }
            await actor.Invoke(context =>
            {
                context.Dictionary.Clear();
                object heap = Get(context, Heap);
                Call(heap, Collect);
                Check((long)Get(Get(heap, Snapshot), Words) == 0);

                return true;
            });
        }
        finally
        {
            release.TrySetResult();
            if (captured is not null)
                await captured.WaitAsync(Deadline);
        }
    }

    internal static async Task ConcurrentDictionary()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        runtime.Modules.Register(
            ProbeModule,
            Live,
            1,
            (_, arguments) => ValueTask.FromResult(arguments[0])
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        _ = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Run,
            Term.I(506)
        ));
        ProcessContext context = await actor.Ready;
        object heap = Get(context, Heap);
        Task writer = System.Threading.Tasks.Task.Run(() =>
        {
            for (int index = 0; index < 64; index++)
            {
                Term key = Term.I(index % 8);
                context.Dictionary[key] = Term.Tuple(Term.I(index), Term.List(Term.I(index + 1)), new Pid(ExternalNode, (ulong)index));
                foreach (var pair in context.Dictionary)
                {
                    RequirePortable(pair.Key);
                    RequirePortable(pair.Value);
                }
                if (index % 3 == 0)
                    context.Dictionary.Remove(key);
            }
        });
        Task collector = System.Threading.Tasks.Task.Run(() =>
        {
            for (int index = 0; index < 64; index++)
                Call(heap, Collect);
        });
        Task info = System.Threading.Tasks.Task.Run(async () =>
        {
            for (int index = 0; index < 64; index++)
            {
                Term observed = await runtime.Modules.Call(
                    context,
                    ErlangModule,
                    ProcessInfo,
                    context.Self,
                    Term.A(DictionaryItem)
                );
                RequirePortable(observed);
            }
        });
        await System.Threading.Tasks.Task.WhenAll(writer, collector, info).WaitAsync(Deadline);
        context.Dictionary.Clear();
        Call(heap, Collect);
        Check((long)Get(Get(heap, Snapshot), Words) == 0);
    }

    internal static async Task DescriptorLockOrder()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        FunctionTerm closure = (FunctionTerm)await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                GuestModule,
                Make,
                Term.I(506)
            ));
        ProcessContext context = await actor.Ready;
        object heap = Get(context, Heap);
        int baseline = (int)Get(Get(heap, Snapshot), Roots);
        object functionsGate = runtime.Functions.GetType().GetField(Gate, Members)!.GetValue(runtime.Functions)!;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task holder = System.Threading.Tasks.Task.Run(() =>
        {
            lock (functionsGate)
            {
                entered.TrySetResult();
                if (!release.Task.Wait(Deadline))
                    throw new TimeoutException(Failure);
            }
        });
        Task<Term>? pending = null;
        try
        {
            await entered.Task.WaitAsync(Deadline);
            pending = actor.InvokeAsync(async current => await closure.Invoke(current, []));
            using var deadline = new CancellationTokenSource(Deadline);
            while (true)
            {
                // The root count proves that the transfer borrow was admitted.
                // Snapshot/collection must remain available while the source
                // recipe capture is blocked on RuntimeFunctionEntries.gate.
                object snapshot = await System.Threading.Tasks.Task.Run(() => Get(heap, Snapshot)).WaitAsync(deadline.Token);
                if ((int)Get(snapshot, Roots) >= baseline + 2)
                    break;
                await System.Threading.Tasks.Task.Delay(1, deadline.Token);
            }
            await System.Threading.Tasks.Task.Run(() => Call(heap, Collect)).WaitAsync(deadline.Token);
            Check(!pending.IsCompleted);
            release.TrySetResult();
            Term value = await pending.WaitAsync(Deadline);
            RequirePortable(value);
        }
        finally
        {
            release.TrySetResult();
            await holder.WaitAsync(Deadline);
            if (pending is not null)
                await pending.WaitAsync(Deadline);
        }
    }

    internal static async Task Errors()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        Term ordinary = await Observe(false);
        Term owned = await Observe(true);
        Check(ordinary.Equals(owned));
        Check(ExternalTermFormat.Encode(ordinary).AsSpan().SequenceEqual(ExternalTermFormat.Encode(owned)));

        async Task<Term> Observe(bool enabled)
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
            artifact.Definition.Register(runtime.Modules);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);

            return await actor.InvokeAsync(async context =>
            {
                try
                {
                    await runtime.Modules.Call(
                        context,
                        GuestModule,
                        FailureEntry,
                        Term.I(506)
                    );
                    throw new InvalidOperationException(Failure);
                }
                catch (ErlangException error)
                {
                    RequirePortable(error.Reason);
                    RequirePortable(error.StackTraceTerm);

                    return Term.Tuple(Term.A(error.ExceptionClass), error.Reason, error.StackTraceTerm);
                }
            });
        }
    }

    internal static async Task PhysicalRetirement()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        object heap = Get(context, Heap);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Term input = Term.Tuple(Term.I(506), Term.List(Term.I(1), Term.I(2)));
        // An opaque managed callback has no source-code lease that could
        // independently make DisposeAsync wait. Only its owned argument roots
        // keep this physical heap alive after the actor's actual body unwinds.
        var callback = new FunctionTerm(
            1,
            async (execution, arguments) =>
        {
            var current = (ProcessContext)execution;
            object invocation = Get(current, CurrentInvocation);
            var rooted = (Term[])Get(invocation, Arguments);
            Check(rooted[0].Equals(arguments[0]));
            Check(rooted[0].GetType().GetProperty(Storage, Members)!.GetValue(rooted[0]) is not null);
            entered.TrySetResult();
            await release.Task; // Deliberately independent of actor cancellation.
            Check(rooted[0].Equals(arguments[0]));
            Check((long)Get(Get(heap, Snapshot), Words) > 0);

            return arguments[0];
        }
        );
        Task<Term> pending = System.Threading.Tasks.Task.Run(async () => await callback.Invoke(context, [input]));
        Task? disposing = null;
        try
        {
            await entered.Task.WaitAsync(Deadline);
            await killer.Invoke(sender =>
 {
     runtime.Exit(sender, actor.Process.Pid, Term.A(Kill));

     return true;
 });
            await ((Task)Get(Get(context, Unwound), TaskProperty)).WaitAsync(Deadline);
            Check(!runtime.IsAlive(actor.Process.Pid));
            // Requires the parent-owned post-logical exit-continuation patch:
            // logical death is visible, but physical Completion retains roots.
            Check(!actor.Process.Completion.IsCompleted);
            Check(!pending.IsCompleted && !((Task)Get(context, Retirement)).IsCompleted);
            disposing = runtime.DisposeAsync().AsTask();
            using var deadline = new CancellationTokenSource(Deadline);
            while (!(bool)typeof(ProcessRuntime).GetField(OwnersClosingField, Members)!.GetValue(runtime)!)
                await System.Threading.Tasks.Task.Delay(1, deadline.Token);
            Check(!disposing.IsCompleted);
            Check((long)Get(Get(heap, Snapshot), Words) > 0);
            release.TrySetResult();
            Term result = await pending.WaitAsync(Deadline);
            RequirePortable(result);
            Check(result.Equals(input));
            await actor.Process.Completion.WaitAsync(Deadline);
            await disposing.WaitAsync(Deadline);
            Check((long)Get(Get(heap, Snapshot), Capacity) == 0);
        }
        finally
        {
            release.TrySetResult();
            await pending.WaitAsync(Deadline);
            if (disposing is not null)
                await disposing.WaitAsync(Deadline);
        }
    }

    internal static async Task LifecycleInfo()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        runtime.Modules.Register(
            ProbeModule,
            Live,
            1,
            (_, arguments) => ValueTask.FromResult(arguments[0])
        );
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        foreach (bool cancel in new[] { false, true })
        {
            var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle process = runtime.Spawn(async context =>
            {
                await runtime.Modules.Call(
                    context,
                    GuestModule,
                    Run,
                    Term.I(506)
                );
                ready.TrySetResult(context);
                Check(await context.ReceiveAsync<Term>(
                    message => message.Equals(Term.A(LifecycleResume)) ? message : null,
                    Deadline
                ) is not null);

                return Nil.Value;
            });
            ProcessContext target = await ready.Task.WaitAsync(Deadline);
            var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task queries = System.Threading.Tasks.Task.Run(async () =>
            {
                for (int index = 0; index < 64; index++)
                {
                    Term info = await observer.InvokeAsync(async context => await runtime.Modules.Call(
                        context,
                        ErlangModule,
                        ProcessInfo,
                        process.Pid,
                        Term.A(DictionaryItem)
                    ));
                    Check(info is Atom { Name: Undefined } || info is TupleTerm { Items.Count: 2 });
                    RequirePortable(info);
                    first.TrySetResult();
                }
            });
            try
            {
                await first.Task.WaitAsync(Deadline);
                if (cancel)
                    await killer.Invoke(context =>
 {
     runtime.Exit(context, process.Pid, Term.A(Kill));

     return true;
 });
                else
                    runtime.Send(process.Pid, Term.A(LifecycleResume));
                await process.Completion.WaitAsync(Deadline);
                await ((Task)Get(Get(target, Unwound), TaskProperty)).WaitAsync(Deadline);
                await queries.WaitAsync(Deadline);
                Term ended = await observer.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    ErlangModule,
                    ProcessInfo,
                    process.Pid,
                    Term.A(DictionaryItem)
                ));
                Check(ended is Atom { Name: Undefined });
                Check((long)Get(Get(Get(target, Heap), Snapshot), Capacity) == 0);
            }
            finally
            {
                runtime.Send(process.Pid, Term.A(LifecycleResume));
                await queries.WaitAsync(Deadline);
            }
        }
    }

    internal static async Task SourceMessageRetirement()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.Ready;
        ProcessContext? senderContext = null;
        Term input = Term.Tuple(Term.I(506), Term.List(Term.I(1), Term.I(2)));
        var sender = runtime.Spawn(async context =>
        {
            senderContext = context;
            await runtime.Modules.Call(
                context,
                GuestModule,
                SendEntry,
                receiver.Process.Pid,
                input
            );

            return Nil.Value;
        });
        await sender.Completion.WaitAsync(Deadline);
        Check((long)Get(Get(Get(senderContext!, Heap), Snapshot), Capacity) == 0);
        Term? received = await receiver.InvokeAsync(async context =>
            await context.ReceiveAsync<Term>(value => value, Deadline));
        Check(received is not null);
        RequirePortable(received!);
        Check(received!.Equals(Term.Tuple(Term.A(MessageTag), input)));
    }

    private static void RequirePortable(Term value)
    {
        var pending = new Stack<Term>();
        pending.Push(value);
        while (pending.TryPop(out Term? current))
        {
            PropertyInfo? storage = current.GetType().GetProperty(Storage, Members);
            Check(storage?.GetValue(current) is null);
            if (current is TupleTerm tuple)
                foreach (Term child in tuple.Items) pending.Push(child);
            else if (current is Cons cons)
            {
                pending.Push(cons.Head);
                pending.Push(cons.Tail);
            }
        }
    }

    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object? Call(object target, string name) => target.GetType().GetMethod(name, Members)!.Invoke(target, []);

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
