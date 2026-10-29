using System.Collections;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.CancellationFailure669Names;
using static Erlang.Tests.ProcessPayloadCore641Access;

namespace Erlang.Tests;

internal static class CancellationFailure669Checks
{
    internal static async Task Run()
    {
        using var compiled = GeneratedModuleCompiler.Compile(Source, SourceFile);
        var runtime = OwnedGuest506RuntimeFactory.Create();
        var failures = new List<Exception>();
        var children = new List<(ProcessHandle Handle, ProcessContext Context)>();
        RuntimeNodeFoundationHostActor? parent = null;
        CancellationTokenRegistration registration = default;
        Task? originalDispose = null;
        var marker = new InvalidOperationException(Marker);
        int callbacks = 0;
        try
        {
            await runtime.InitializeModulesAsync(_ => { });
            compiled.Definition.Register(runtime.Modules);
            Check(ReferenceEquals(runtime.Modules.SourceOwner(Module, Start, 1), compiled.Definition));
            parent = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext ownerContext = await parent.Ready.WaitAsync(Deadline);
            registration = ownerContext.Cancellation.Register(() =>
            {
                Interlocked.Increment(ref callbacks);
                throw marker;
            });
            Term pids = await parent.InvokeAsync(async context =>
            {
                Term started = await runtime.Modules.Call(
                    context,
                    Module,
                    Start,
                    context.Self
                );
                Pid[] ids = Cons.Items(started).Cast<Pid>().ToArray();
                Check(ids.Length == 2 && ids.Distinct().Count() == 2);
                var ready = new HashSet<Pid>();
                for (int index = 0; index < ids.Length; index++)
                {
                    Term? message = await context.ReceiveAsync<Term>(term => term is TupleTerm
                    {
                        Items: [Atom { Name: Ready }, Pid child]
                    } && ids.Contains(child) ? term : null, Deadline);
                    Check(message is TupleTerm { Items: [_, Pid] });
                    Check(ready.Add((Pid)((TupleTerm)message!).Items[1]));
                }

                return started;
            }).WaitAsync(Deadline);
            lock (Field(runtime, ProcessPayloadCore641Names.Gate)!)
            {
                var table = (Dictionary<Pid, ProcessContext>)Field(runtime, Processes)!;
                foreach (Pid id in Cons.Items(pids).Cast<Pid>())
                {
                    ProcessContext context = table[id];
                    Check(Field(context, Input) is not null);
                    ProcessHandle handle = runtime.GetProcessHandle(id);
                    Check(!handle.Completion.IsCompleted);
                    children.Add((handle, context));
                }
            }
            object owner = Property(runtime, ProcessPayloadCore641Names.Owner);
            lock (Field(owner, ProcessPayloadCore641Names.Gate)!)
            {
                object[] live = ((IEnumerable)Field(owner, ProcessPayloadCore641Names.Live)!).Cast<object>().ToArray();
                foreach (var child in children)
                    Check(live.Any(work => ReferenceEquals(Property(work, Context), child.Context)
                        && (int)Field(work, Ended)! == 0));
            }

            originalDispose = runtime.DisposeAsync().AsTask();
            await Fault(originalDispose, marker);
            Check(originalDispose.IsCompleted && Volatile.Read(ref callbacks) == 1);
            foreach (var child in children)
            {
                await child.Handle.Completion.WaitAsync(Deadline);
                await ((TaskCompletionSource)Property(child.Context, Unwound)).Task.WaitAsync(Deadline);
                Check(child.Context.Cancellation.IsCancellationRequested);
            }
            lock (Field(owner, ProcessPayloadCore641Names.Gate)!)
                Check(!((IEnumerable)Field(owner, ProcessPayloadCore641Names.Live)!).Cast<object>().Any());
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // A second PUBLIC close is cleanup, not the test's success path.
            // It can settle the old implementation's interrupted first loop;
            // the original timeout/failure remains in failures above.
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error) when (OnlyExpected(error, marker)) { }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (originalDispose is not null)
                try
                {
                    await originalDispose.WaitAsync(Deadline);
                }
                catch (Exception error) when (OnlyExpected(error, marker)) { }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            foreach (var child in children)
            {
                try
                {
                    await child.Handle.Completion.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                try
                {
                    await ((TaskCompletionSource)Property(child.Context, Unwound)).Task.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
            if (parent is not null)
                try
                {
                    await parent.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error) when (OnlyExpected(error, marker)) { }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                registration.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(Failure, failures);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
