using System.Reflection;
using Erlang;

namespace Erlang.Tests;

public static class ModuleRegistryLeaseChecks
{
    public static async Task Run()
    {
        await TracingHalt();
        await BodyCompletion(false, false);
        await BodyCompletion(false, true);
        await BodyCompletion(true, false);
        await BodyCompletion(true, true);
    }

    private static bool Executing(ProcessContext context, ModuleCodeImage image)
    {
        MethodInfo method = typeof(ProcessContext).GetMethod(ModuleRegistryLeaseCheckNames.Executing, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(ModuleRegistryLeaseCheckNames.MetadataMissing);

        return (bool)method.Invoke(context, [image])!;
    }

    private static async Task TracingHalt()
    {
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);

            return await release.Task.WaitAsync(context.Cancellation);
        });
        ProcessContext context = await ready.Task;
        int bodyCalls = 0;
        ErlangFunction body = (_, _) =>
        {
            Interlocked.Increment(ref bodyCalls);

            return ValueTask.FromResult<Term>(ModuleRegistryLeaseCheckNames.Result);
        };
        runtime.Modules.Register(
            ModuleRegistryLeaseCheckNames.Module,
            ModuleRegistryLeaseCheckNames.Function,
            0,
            body
        );
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(ModuleRegistryLeaseCheckNames.Module)!;
        object gate = typeof(ProcessRuntime).GetField(ModuleRegistryLeaseCheckNames.Gate, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(runtime)
            ?? throw new InvalidOperationException(ModuleRegistryLeaseCheckNames.MetadataMissing);
        Task? call = null;
        try
        {
            Monitor.Enter(gate);
            try
            {
                call = Task.Run(async () =>
                {
                    try
                    {
                        _ = await runtime.Modules.Call(context, ModuleRegistryLeaseCheckNames.Module, ModuleRegistryLeaseCheckNames.Function);
                        throw new InvalidOperationException(ModuleRegistryLeaseCheckNames.WrongHalt);
                    }
                    catch (ProcessHaltTransfer transfer) when (ReferenceEquals(transfer.Runtime, runtime))
                    {
                        Require(bodyCalls == 0, ModuleRegistryLeaseCheckNames.BodyEntered);
                        Require(!Executing(context, image), ModuleRegistryLeaseCheckNames.LeaseLeaked);
                        Require(context.ExecutingSourceImage(body) is null, ModuleRegistryLeaseCheckNames.FlowLeaked);
                    }
                });
                bool acquired = SpinWait.SpinUntil(() => Executing(context, image), TimeSpan.FromSeconds(ModuleRegistryLeaseCheckNames.DeadlineSeconds));
                Require(acquired, ModuleRegistryLeaseCheckNames.LeaseMissing);
                _ = runtime.RequestImmediateHalt(ModuleRegistryLeaseCheckNames.HaltStatus);
            }
            finally
            {
                Monitor.Exit(gate);
            }
            await call.WaitAsync(TimeSpan.FromSeconds(ModuleRegistryLeaseCheckNames.DeadlineSeconds));
        }
        finally
        {
            _ = runtime.RequestImmediateHalt(ModuleRegistryLeaseCheckNames.HaltStatus);
            release.TrySetResult(ModuleRegistryLeaseCheckNames.Result);
            if (call is not null)
                await Drain(call);
            _ = await owner.Completion.WaitAsync(TimeSpan.FromSeconds(ModuleRegistryLeaseCheckNames.DeadlineSeconds));
            await runtime.HaltQuiescence.WaitAsync(TimeSpan.FromSeconds(ModuleRegistryLeaseCheckNames.DeadlineSeconds));
        }
    }

    private static async Task BodyCompletion(bool asynchronous, bool fail)
    {
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);

            return await release.Task;
        });
        ProcessContext context = await ready.Task;
        ErlangFunction? body = null;
        ModuleCodeImage? image = null;

        void CheckBody(ProcessContext caller)
        {
            Require(
                Executing(caller, image!) && ReferenceEquals(caller.ExecutingSourceImage(body!), image),
                ModuleRegistryLeaseCheckNames.InBodyLeaseMissing
            );
            if (fail)
                throw new InvalidOperationException(ModuleRegistryLeaseCheckNames.BodyFailure);
        }

        async ValueTask<Term> AsyncBody(ProcessContext caller)
        {
            await Task.Yield();
            CheckBody(caller);

            return ModuleRegistryLeaseCheckNames.Result;
        }
        body = (caller, _) =>
        {
            if (asynchronous)
                return AsyncBody(caller);
            CheckBody(caller);

            return ValueTask.FromResult<Term>(ModuleRegistryLeaseCheckNames.Result);
        };
        runtime.Modules.Register(
            ModuleRegistryLeaseCheckNames.Module,
            ModuleRegistryLeaseCheckNames.Function,
            0,
            body
        );
        image = runtime.Modules.CurrentCodeImage(ModuleRegistryLeaseCheckNames.Module)!;
        try
        {
            try
            {
                _ = await runtime.Modules.Call(context, ModuleRegistryLeaseCheckNames.Module, ModuleRegistryLeaseCheckNames.Function);
                Require(!fail, ModuleRegistryLeaseCheckNames.UnexpectedSuccess);
            }
            catch (InvalidOperationException error) when (fail && error.Message == ModuleRegistryLeaseCheckNames.BodyFailure)
            {
            }
            Require(!Executing(context, image), ModuleRegistryLeaseCheckNames.LeaseLeaked);
            Require(context.ExecutingSourceImage(body) is null, ModuleRegistryLeaseCheckNames.FlowLeaked);
        }
        finally
        {
            release.TrySetResult(ModuleRegistryLeaseCheckNames.Result);
            _ = await owner.Completion;
        }
    }

    private static async Task Drain(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(ModuleRegistryLeaseCheckNames.DeadlineSeconds));
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
