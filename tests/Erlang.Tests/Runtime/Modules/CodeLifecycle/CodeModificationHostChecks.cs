using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

public static class CodeModificationHostChecks
{
    public static async Task Run()
    {
        await WriterAndReadProgress();
        await CancelledAdmission();
        await CancelledGuestWriter();
        await ChildDoesNotInheritAuthority();
        await DisposalWithinMutation();
        ScopeOrderAndRollback();
    }

    private static object Permission(ModuleRegistry registry) => typeof(ModuleRegistry).GetProperty(CodeModificationHostNames.Permission, CodeModificationHostNames.Members)!.GetValue(registry)!;

    private static object? Call(object target, string method, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, CodeModificationHostNames.Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static Task<object> Acquire(object permission, CancellationToken cancellation = default)
    {
        object pending = Call(permission, CodeModificationHostNames.Acquire, cancellation)!;
        Task task = (Task)pending.GetType().GetMethod(CodeModificationHostNames.AsTask)!.Invoke(pending, null)!;

        return AwaitTicket(task);
    }

    private static async Task<object> AwaitTicket(Task task)
    {
        await task;

        return task.GetType().GetProperty(CodeModificationHostNames.Result)!.GetValue(task)!;
    }

    private static T WithTicket<T>(object ticket, Func<T> operation)
    {
        try
        {
            return (T)ticket.GetType().GetMethod(CodeModificationHostNames.Run, CodeModificationHostNames.Members)!.MakeGenericMethod(typeof(T)).Invoke(ticket, [operation])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static async Task WaitWriter(object permission, string property = CodeModificationHostNames.Pending)
    {
        using var deadline = new CancellationTokenSource(CodeModificationHostNames.Deadline);
        while ((int)permission.GetType().GetProperty(property, CodeModificationHostNames.Members)!.GetValue(permission)! == 0)
        {
            deadline.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    private static async Task WriterAndReadProgress()
    {
        var registry = new ModuleRegistry();
        registry.PublishModule(CodeModificationHostNames.Module);
        object permission = Permission(registry);
        var ticket = (IDisposable)await Acquire(permission).WaitAsync(CodeModificationHostNames.Deadline);
        Task writer = Task.Run(() => registry.Apply(target =>
        {
            target.Register(
                CodeModificationHostNames.Other,
                CodeModificationHostNames.Function,
                0,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            target.PublishModuleCode(CodeModificationHostNames.Other, Descriptor());
        }));
        try
        {
            await WaitWriter(permission).WaitAsync(CodeModificationHostNames.Deadline);
            Require(!writer.IsCompleted && !registry.IsModuleLoaded(CodeModificationHostNames.Other));
            var read = Task.Run(() => registry.CurrentCodeImage(CodeModificationHostNames.Module));
            Require(await read.WaitAsync(CodeModificationHostNames.Deadline) is not null);
            WithTicket(ticket, () =>
            {
                registry.PublishModule(CodeModificationHostNames.Module);

                return true;
            });
        }
        finally
        {
            ticket.Dispose();
            await writer.WaitAsync(CodeModificationHostNames.Deadline);
        }
        Require(registry.HasExport(CodeModificationHostNames.Other, CodeModificationHostNames.Function, 0));
    }

    private static async Task CancelledAdmission()
    {
        object permission = Permission(new ModuleRegistry());
        var owner = (IDisposable)await Acquire(permission).WaitAsync(CodeModificationHostNames.Deadline);
        using var cancellation = new CancellationTokenSource();
        Task<object> blocked = Acquire(permission, cancellation.Token);
        Require(!blocked.IsCompleted);
        cancellation.Cancel();
        try
        {
            await blocked.WaitAsync(CodeModificationHostNames.Deadline);
            Require(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            owner.Dispose();
        }
        using var next = (IDisposable)await Acquire(permission).WaitAsync(CodeModificationHostNames.Deadline);
    }

    private static async Task ChildDoesNotInheritAuthority()
    {
        var registry = new ModuleRegistry();
        object permission = Permission(registry);
        var ticket = (IDisposable)await Acquire(permission).WaitAsync(CodeModificationHostNames.Deadline);
        Task child = WithTicket(ticket, () => Task.Run(() => registry.PublishModule(CodeModificationHostNames.Module)));
        try
        {
            await WaitWriter(permission).WaitAsync(CodeModificationHostNames.Deadline);
            Require(!child.IsCompleted);
        }
        finally
        {
            ticket.Dispose();
            await child.WaitAsync(CodeModificationHostNames.Deadline);
        }
    }

    private static async Task CancelledGuestWriter()
    {
        await using var runtime = new ProcessRuntime();
        runtime.Modules.PublishModule(CodeModificationHostNames.Module);
        object permission = Permission(runtime.Modules);
        var ticket = (IDisposable)await Acquire(permission).WaitAsync(CodeModificationHostNames.Deadline);
        var victim = runtime.Spawn(context => runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            CodeModificationHostNames.DeleteModule,
            Term.A(CodeModificationHostNames.Module)
        ));
        try
        {
            await WaitWriter(permission, CodeModificationHostNames.PendingAsync).WaitAsync(CodeModificationHostNames.Deadline);
            var killer = runtime.Spawn(context =>
            {
                runtime.Exit(context, victim.Pid, Term.A(ProcessExitSignals.Kill));

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            await killer.Completion.WaitAsync(CodeModificationHostNames.Deadline);
            Term down = await victim.Completion.WaitAsync(CodeModificationHostNames.Deadline);
            Require(down.Equals(Term.A(ProcessExitReasons.Killed)) && runtime.Modules.IsModuleLoaded(CodeModificationHostNames.Module));
        }
        finally
        {
            ticket.Dispose();
        }
        using var next = (IDisposable)await Acquire(permission).WaitAsync(CodeModificationHostNames.Deadline);
        Require(runtime.Modules.IsModuleLoaded(CodeModificationHostNames.Module));
    }

    private static async Task DisposalWithinMutation()
    {
        var registry = new ModuleRegistry();
        object permission = Permission(registry);
        var ticket = (IDisposable)await Acquire(permission).WaitAsync(CodeModificationHostNames.Deadline);
        Task<object>? next = null;
        WithTicket(
            ticket,
            () =>
        {
            ticket.Dispose();
            next = Task.Run(() => Acquire(permission));
            Require(!(bool)permission.GetType().GetProperty(CodeModificationHostNames.Available, CodeModificationHostNames.Members)!.GetValue(permission)!);
            Require(!next.IsCompleted);
            registry.PublishModule(CodeModificationHostNames.Module);

            return true;
        }
        );
        using var later = (IDisposable)await next!.WaitAsync(CodeModificationHostNames.Deadline);
    }

    private static void ScopeOrderAndRollback()
    {
        var registry = new ModuleRegistry();
        object permission = Permission(registry);
        var outer = (IDisposable)Call(permission, CodeModificationHostNames.Enter)!;
        var inner = (IDisposable)Call(permission, CodeModificationHostNames.Enter)!;
        bool rejected = false;
        try
        {
            outer.Dispose();
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        inner.Dispose();
        outer.Dispose();
        Require(rejected);
        rejected = false;
        object gate = typeof(ModuleRegistry).GetField(CodeModificationHostNames.RegistryGate, CodeModificationHostNames.Members)!.GetValue(registry)!;
        lock (gate)
        {
            try
            {
                using var invalid = (IDisposable)Call(permission, CodeModificationHostNames.Enter)!;
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
        }
        Require(rejected);
        try
        {
            registry.Apply(target =>
            {
                target.PublishModule(CodeModificationHostNames.Other);
                throw new InvalidOperationException();
            });
        }
        catch (InvalidOperationException)
        {
        }
        Require(!registry.IsModuleLoaded(CodeModificationHostNames.Other));
        registry.PublishModule(CodeModificationHostNames.Module);
    }

    private static ModuleCodeDescriptor Descriptor() => new(
        [(CodeModificationHostNames.Function, 0)],
        [],
        new BitString(new byte[System.Security.Cryptography.MD5.HashSizeInBytes]),
        Nil.Value
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(CodeModificationHostNames.Broken);
    }
}
