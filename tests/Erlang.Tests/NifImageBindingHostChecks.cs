using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

public static class NifImageBindingHostChecks
{
    public static async Task Run()
    {
        await LazyConcurrentIdentity();
        await EligibilityAndPublication();
        await ImageIsolationAndAbortRetry();
        await RetiredInitializer();
        await ActiveCallQuiescence();
        await FailedCleanup();
    }

    private static async Task LazyConcurrentIdentity()
    {
        var registry = new ModuleRegistry();
        registry.PublishModule(NifImageBindingHostNames.Module);
        var image = registry.CurrentCodeImage(NifImageBindingHostNames.Module)!;
        Type store = typeof(ModuleCodeImage).Assembly.GetType(NifImageBindingHostNames.BindingStore)!;
        MethodInfo created = store.GetMethod(NifImageBindingHostNames.IsCreated, BindingFlags.Static | BindingFlags.NonPublic)!;
        Require(!(bool)created.Invoke(null, [image])!);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PropertyInfo property = typeof(ModuleCodeImage).GetProperty(NifImageBindingHostNames.Table, NifImageBindingHostNames.Members)!;
        Task<object>[] contenders = Enumerable.Range(0, NifImageBindingHostNames.ConcurrentReaders).Select(_ => Task.Run(async () =>
        {
            await start.Task;

            return property.GetValue(image)!;
        })).ToArray();
        start.TrySetResult();
        object[] tables = await Task.WhenAll(contenders).WaitAsync(NifImageBindingHostNames.Deadline);
        Require(tables.All(table => ReferenceEquals(table, tables[0])));
        Require((bool)created.Invoke(null, [image])!);
        await ((Task)Call(tables[0], NifImageBindingHostNames.Retire)!).WaitAsync(NifImageBindingHostNames.Deadline);
    }

    private static object Table(ModuleRegistry registry)
    {
        registry.PublishModule(NifImageBindingHostNames.Module);
        var image = registry.CurrentCodeImage(NifImageBindingHostNames.Module)!;

        return typeof(ModuleCodeImage).GetProperty(NifImageBindingHostNames.Table, NifImageBindingHostNames.Members)!.GetValue(image)!;
    }

    private static object? Call(object target, string method, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, NifImageBindingHostNames.Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static ModuleCodeDescriptor Descriptor(IReadOnlyList<(string Name, int Arity)>? declarations = null) => new(
        [(NifImageBindingHostNames.Function, 0)],
        [],
        new BitString(new byte[System.Security.Cryptography.MD5.HashSizeInBytes]),
        Nil.Value
    )
    {
        DeclaredNifs = declarations
    };

    private static KeyValuePair<(string Name, int Arity), ErlangFunction>[] Bindings(ErlangFunction body) =>
        [new((NifImageBindingHostNames.Function, 0), body)];

    private static async Task EligibilityAndPublication()
    {
        var registry = new ModuleRegistry();
        object table = Table(registry);
        object pending = Call(table, NifImageBindingHostNames.Begin, Descriptor())!;
        Require(Call(
            table,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Function,
            0
        ) is null);
        Reject<InvalidOperationException>(() => Call(table, NifImageBindingHostNames.Begin, Descriptor()));
        var owner = new NifImageBindingTestOwner();
        var bindings = Bindings((_, _) => ValueTask.FromResult<Term>(Term.I(NifImageBindingHostNames.Expected)));
        Reject<ArgumentException>(() => Call(
            pending,
            NifImageBindingHostNames.Publish,
            bindings.Concat(bindings).ToArray(),
            owner
        ));
        Require(owner.Calls == 0);
        Call(
            pending,
            NifImageBindingHostNames.Publish,
            bindings,
            owner
        );
        bindings[0] = new((NifImageBindingHostNames.Other, 0), (_, _) => throw new InvalidOperationException());
        ((IDisposable)pending).Dispose();
        Require(Call(
            table,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Other,
            0
        ) is null);
        using var admitted = (IDisposable)Call(
            table,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Function,
            0
        )!;
        admitted.Dispose();
        var retirement = (Task)Call(table, NifImageBindingHostNames.Retire)!;
        Require(ReferenceEquals(retirement, Call(table, NifImageBindingHostNames.Retire)));
        await retirement.WaitAsync(NifImageBindingHostNames.Deadline);
        Require(owner.Calls == 1);
        Reject<InvalidOperationException>(() => Call(
            table,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Function,
            0
        ));

        foreach (var declarations in new IReadOnlyList<(string Name, int Arity)>[] { [], [(NifImageBindingHostNames.Other, 0)] })
        {
            object restricted = Table(new ModuleRegistry());
            using var reservation = (IDisposable)Call(restricted, NifImageBindingHostNames.Begin, Descriptor(declarations))!;
            var rejectedOwner = new NifImageBindingTestOwner();
            Reject<ArgumentException>(() => Call(
                reservation,
                NifImageBindingHostNames.Publish,
                Bindings((_, _) => ValueTask.FromResult<Term>(Nil.Value)),
                rejectedOwner
            ));
            Require(rejectedOwner.Calls == 0);
            await rejectedOwner.DisposeAsync();
            reservation.Dispose();
            await ((Task)Call(restricted, NifImageBindingHostNames.Retire)!).WaitAsync(NifImageBindingHostNames.Deadline);
            Require(rejectedOwner.Calls == 1);
        }
    }

    private static async Task RetiredInitializer()
    {
        object table = Table(new ModuleRegistry());
        var pending = (IDisposable)Call(table, NifImageBindingHostNames.Begin, Descriptor())!;
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifImageBindingTestOwner(async () =>
        {
            cleanupEntered.TrySetResult();
            await release.Task;
        });
        Task retirement = (Task)Call(table, NifImageBindingHostNames.Retire)!;
        Require(!retirement.IsCompleted);
        Reject<InvalidOperationException>(() => Call(
            pending,
            NifImageBindingHostNames.Publish,
            Bindings((_, _) => ValueTask.FromResult<Term>(Nil.Value)),
            owner
        ));
        Task cleanup = owner.DisposeAsync().AsTask();
        await cleanupEntered.Task.WaitAsync(NifImageBindingHostNames.Deadline);
        Require(!retirement.IsCompleted);
        release.TrySetResult();
        await cleanup.WaitAsync(NifImageBindingHostNames.Deadline);
        pending.Dispose();
        await retirement.WaitAsync(NifImageBindingHostNames.Deadline);
        Require(owner.Calls == 1);
    }

    private static async Task ImageIsolationAndAbortRetry()
    {
        var registry = new ModuleRegistry();
        object oldTable = Table(registry);
        var aborted = (IDisposable)Call(oldTable, NifImageBindingHostNames.Begin, Descriptor())!;
        aborted.Dispose();
        object reservation = Call(oldTable, NifImageBindingHostNames.Begin, Descriptor([(NifImageBindingHostNames.Function, 0)]))!;
        var oldOwner = new NifImageBindingTestOwner();
        Call(
            reservation,
            NifImageBindingHostNames.Publish,
            Bindings((_, _) => ValueTask.FromResult<Term>(Nil.Value)),
            oldOwner
        );
        registry.PublishModuleCode(NifImageBindingHostNames.Module, Descriptor());
        registry.DeleteModule(NifImageBindingHostNames.Module);
        object currentTable = Table(registry);
        Require(!ReferenceEquals(oldTable, currentTable));
        Require(Call(
            currentTable,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Function,
            0
        ) is null);
        ((IDisposable)Call(
            oldTable,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Function,
            0
        )!).Dispose();
        var currentOwner = new NifImageBindingTestOwner();
        object currentReservation = Call(currentTable, NifImageBindingHostNames.Begin, Descriptor())!;
        Call(
            currentReservation,
            NifImageBindingHostNames.Publish,
            Bindings((_, _) => ValueTask.FromResult<Term>(Nil.Value)),
            currentOwner
        );
        await ((Task)Call(oldTable, NifImageBindingHostNames.Retire)!).WaitAsync(NifImageBindingHostNames.Deadline);
        Require(oldOwner.Calls == 1 && currentOwner.Calls == 0);
        ((IDisposable)Call(
            currentTable,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Function,
            0
        )!).Dispose();
        await ((Task)Call(currentTable, NifImageBindingHostNames.Retire)!).WaitAsync(NifImageBindingHostNames.Deadline);
        Require(currentOwner.Calls == 1);
    }

    private static async Task ActiveCallQuiescence()
    {
        await using var runtime = new ProcessRuntime();
        object table = Table(runtime.Modules);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifImageBindingTestOwner();
        object pending = Call(table, NifImageBindingHostNames.Begin, Descriptor())!;
        Call(
            pending,
            NifImageBindingHostNames.Publish,
            Bindings(async (_, _) =>
        {
            entered.TrySetResult();
            await release.Task;

            return Term.I(NifImageBindingHostNames.Expected);
        }),
            owner
        );
        var admitted = (IDisposable)Call(
            table,
            NifImageBindingHostNames.Admit,
            NifImageBindingHostNames.Function,
            0
        )!;
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                var invocation = (ValueTask<Term>)Call(
                    admitted,
                    NifImageBindingHostNames.Invoke,
                    context,
                    Array.Empty<Term>()
                )!;
                Term result = await invocation;
                Require(result.Equals(Term.I(NifImageBindingHostNames.Expected)));

                return result;
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        try
        {
            await entered.Task.WaitAsync(NifImageBindingHostNames.Deadline);
            admitted.Dispose();
            Task retirement = (Task)Call(table, NifImageBindingHostNames.Retire)!;
            Require(!retirement.IsCompleted && owner.Calls == 0);
            release.TrySetResult();
            await process.Completion.WaitAsync(NifImageBindingHostNames.Deadline);
            await retirement.WaitAsync(NifImageBindingHostNames.Deadline);
            Require(owner.Calls == 1 && failure is null);
        }
        finally
        {
            release.TrySetResult();
            admitted.Dispose();
            await ((Task)Call(table, NifImageBindingHostNames.Retire)!).WaitAsync(NifImageBindingHostNames.Deadline);
        }
    }

    private static async Task FailedCleanup()
    {
        object table = Table(new ModuleRegistry());
        object pending = Call(table, NifImageBindingHostNames.Begin, Descriptor())!;
        var owner = new NifImageBindingTestOwner(() => throw new InvalidOperationException(NifImageBindingHostNames.CleanupFailure));
        Call(
            pending,
            NifImageBindingHostNames.Publish,
            Bindings((_, _) => ValueTask.FromResult<Term>(Nil.Value)),
            owner
        );
        Task retirement = (Task)Call(table, NifImageBindingHostNames.Retire)!;
        try
        {
            await retirement;
            Require(false);
        }
        catch (InvalidOperationException failure) when (failure.Message == NifImageBindingHostNames.CleanupFailure)
        {
            Require(owner.Calls == 1 && ReferenceEquals(retirement, Call(table, NifImageBindingHostNames.Retire)));
        }
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(NifImageBindingHostNames.Broken);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifImageBindingHostNames.Broken);
    }
}
