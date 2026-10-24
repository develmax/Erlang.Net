using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class NifAdmissionHostBridge
{
    internal static object Property(object target, string name) => target.GetType().GetProperty(name, NifAdmissionHostNames.Members)!.GetValue(target)!;

    internal static object? Call(object target, string method, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, NifAdmissionHostNames.Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } failure)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }

    internal static async Task<object> Reserve(ModuleRegistry registry, ProcessContext context)
    {
        object pending = Call(registry, NifAdmissionHostNames.Reserve, context)!;
        Task task = (Task)pending.GetType().GetMethod(NifAdmissionHostNames.AsTask)!.Invoke(pending, null)!;
        await task;

        return task.GetType().GetProperty(NifAdmissionHostNames.Result)!.GetValue(task)!;
    }

    internal static object Initialized(IAsyncDisposable owner, IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> bindings)
    {
        Type type = typeof(ModuleRegistry).Assembly.GetType(NifAdmissionHostNames.InitializedType)!;

        return Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [owner, bindings],
            null
        )!;
    }

    internal static Task Initialize(object reservation, Func<CancellationToken, ValueTask<object>> factory)
    {
        Type type = typeof(ModuleRegistry).Assembly.GetType(NifAdmissionHostNames.InitializedType)!;
        var initializer = typeof(NifAdmissionHostBridge).GetMethod(NifAdmissionHostNames.FactoryMethod, BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(type).Invoke(null, [factory]);

        return (Task)Call(reservation, NifAdmissionHostNames.Initialize, initializer)!;
    }

    private static Func<CancellationToken, ValueTask<T>> MakeInitializer<T>(Func<CancellationToken, ValueTask<object>> factory) => async cancellation => (T)await factory(cancellation);

    internal static async Task Failure(Task operation, string expected)
    {
        try
        {
            await operation.WaitAsync(NifAdmissionHostNames.Deadline);
        }
        catch (InvalidOperationException error) when (error.GetType().GetProperty(NifAdmissionHostNames.Failure, NifAdmissionHostNames.Members)?.GetValue(error)?.ToString() == expected)
        {
            return;
        }
        throw new InvalidOperationException(NifAdmissionHostNames.Broken);
    }

    internal static async Task Dispose(object reservation) => await ((IAsyncDisposable)reservation).DisposeAsync();

    internal static async Task Context(ProcessRuntime runtime, Func<ProcessContext, Task> operation)
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = runtime.Spawn(async context =>
        {
            try
            {
                await operation(context);
                completed.TrySetResult();
            }
            catch (Exception failure)
            {
                completed.TrySetException(failure);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(NifAdmissionHostNames.Deadline);
        await handle.Completion.WaitAsync(NifAdmissionHostNames.Deadline);
    }

    internal static async Task WaitWriter(ModuleRegistry registry)
    {
        object permission = Property(registry, NifAdmissionHostNames.Permission);
        using var deadline = new CancellationTokenSource(NifAdmissionHostNames.Deadline);
        while ((int)Property(permission, NifAdmissionHostNames.Waiting) == 0)
        {
            deadline.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    internal static Task Retire(ModuleCodeImage image) => (Task)Call(Property(image, NifAdmissionHostNames.NifBindings), NifAdmissionHostNames.Retire)!;
}
