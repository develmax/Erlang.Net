using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class WorkerLifecycleAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(WorkerLifecycleNames.Failure);
    }

    internal static object? Read(object? target, string name)
    {
        if (target is null)
            return null;
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, Members | BindingFlags.DeclaredOnly)
                ?? type.GetField("<" + name + ">k__BackingField", Members | BindingFlags.DeclaredOnly);
            if (field is not null)
                return field.GetValue(target);
        }

        throw new MissingFieldException(target.GetType().FullName, name);
    }

    internal static object Call(object target, string name, params object[] args)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, args)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static Type RuntimeType(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    internal static object Create(string name, params object?[] args)
    {
        try
        {
            return Activator.CreateInstance(
                RuntimeType(name),
                Members,
                null,
                args,
                null
            )!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static object Policy(Action<string, int>? start = null, Action<string, int>? entered = null,
        Action<string, object>? checkpoint = null) => Create(
            WorkerLifecycleNames.Policy,
            start,
            entered,
            checkpoint
        );

    internal static async Task<Exception> MustFail(Task operation, params string[] markers)
    {
        Exception? failure = null;
        try
        {
            await operation.WaitAsync(WorkerLifecycleNames.Deadline);
        }
        catch (Exception error) when (error is not TimeoutException)
        {
            failure = error;
        }
        if (failure is null)
            throw new InvalidOperationException(WorkerLifecycleNames.MissingFailure);
        foreach (string marker in markers)
            if (!failure.ToString().Contains(marker, StringComparison.Ordinal))
                throw new InvalidOperationException(WorkerLifecycleNames.MissingMarker + marker, failure);

        return failure!;
    }

    internal static Task Ready(object worker, bool dirty)
    {
        object value = Read(worker, dirty ? "Started" : "ready")!;

        return (Task)value.GetType().GetProperty("Task")!.GetValue(value)!;
    }

    internal static void Joined(object owner, bool dirty)
    {
        foreach (object worker in (Array)Read(owner, dirty ? "workers522" : "normalWorkers")!)
        {
            Thread thread = (Thread)Read(worker, "thread")!;
            Require(!thread.IsAlive);
            bool started = (bool)Read(worker, "started")!;
            if (started)
                Require(thread.Join(0));
            if (dirty)
            {
                Require((bool)Read(worker, "joined522")!);
                Require((int)Read(worker, "releasedQueue522")! == 1);
            }
            else
            {
                Require((bool)Read(worker, "wakeDisposed")!);
                Require(Read(worker, "join") is Task { IsCompleted: true });
            }
        }
    }

    internal static void RuntimeResources(object runtime)
    {
        if (Read(runtime, "actorDispatcher") is { } dispatcher)
            Joined(dispatcher, false);
        if (Read(runtime, "dirtyNative419") is { } dirty)
            Joined(dirty, true);
        if ((bool)Read(runtime, "ownsStandardStreams")! && Read(runtime, "StandardStreams") is { } streams)
            Require((bool)Read(streams, "disposed")!);
        if (Read(runtime, "OsEnvironment") is { } environment)
        {
            Require((bool)Read(environment, "closed")!);
            Require((bool)Read(environment, "driverStorageReleased422")!);
            Require(Read(environment, "locale") is System.Runtime.InteropServices.SafeHandle { IsClosed: true });
        }
        object ownersGate = Read(runtime, "ownedTermOwnersGate506")!;
        lock (ownersGate)
            Require(((IDictionary)Read(runtime, "ownedTermOwners506")!).Count == 0);
        if (Read(runtime, "initializer") is { } initializer)
            Require(Read(initializer, "Completion") is Task { IsCompleted: true });
    }
}
