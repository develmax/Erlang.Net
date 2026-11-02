using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class SourcePurgeStageHostBridge
{
    internal static object Property(object target, string name)
        => target.GetType().GetProperty(name, SourcePurgeStageHostNames.Members)!.GetValue(target)!;

    internal static object? Call(object target, string name, params object?[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(SourcePurgeStageHostNames.Members)
            .Single(value => value.Name == name && !value.IsGenericMethod && value.GetParameters().Length == arguments.Length);

        return Invoke(method, target, arguments);
    }

    private static object? Invoke(MethodBase method, object? target, object?[] arguments)
    {
        try
        {
            return method is ConstructorInfo constructor ? constructor.Invoke(arguments) : ((MethodInfo)method).Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    internal static bool Is(Exception failure, string reason)
        => failure.GetType().FullName == SourcePurgeStageHostNames.AdmissionType
            && Property(failure, SourcePurgeStageHostNames.Failure).ToString() == reason;

    internal static object Prepare(ModuleRegistry registry)
        => Call(registry, SourcePurgeStageHostNames.Prepare, SourcePurgeStageHostNames.Module)!;

    internal static ModuleCodeImage Image(object session)
        => session is ModuleCodeImage legacy ? legacy : (ModuleCodeImage)Property(session, SourcePurgeStageHostNames.Image);

    internal static bool Finish(ModuleRegistry registry, object session, bool complete = false)
        => session is ModuleCodeImage legacy
            ? LegacyFinish(registry, legacy, complete)
            : (bool)Call(
                registry,
                SourcePurgeStageHostNames.Finish,
                session,
                complete
            )!;

    private static bool LegacyFinish(ModuleRegistry registry, ModuleCodeImage image, bool complete)
    {
        Call(
            registry,
            SourcePurgeStageHostNames.Finish,
            SourcePurgeStageHostNames.Module,
            image,
            complete
        );

        return complete;
    }

    internal static Task PrepareAsync(ModuleRegistry registry, ProcessContext context)
    {
        object pending = Call(
            registry,
            SourcePurgeStageHostNames.PrepareAsync,
            context,
            SourcePurgeStageHostNames.Module
        )!;

        return (Task)pending.GetType().GetMethod(SourcePurgeStageHostNames.AsTask)!.Invoke(pending, null)!;
    }

    internal static ValueTask<bool> FinishAsync(ModuleRegistry registry, object session, bool complete = false)
        => (ValueTask<bool>)Call(
            registry,
            SourcePurgeStageHostNames.FinishAsync,
            session,
            complete
        )!;

    internal static ValueTask<bool> Publish(ModuleRegistry registry, ProcessContext context, Action publication)
    {
        MethodInfo method = typeof(ModuleRegistry).GetMethods(SourcePurgeStageHostNames.Members)
            .Single(value => value.Name == SourcePurgeStageHostNames.ModifySource
                && value.GetParameters()[0].ParameterType == typeof(ProcessContext)).MakeGenericMethod(typeof(bool));

        return (ValueTask<bool>)Invoke(method, registry, [context, (Func<bool>)(() =>
 {
 publication();

 return true;
 })])!;
    }

    internal static object SourcePermission(ModuleRegistry registry) => Property(registry, SourcePurgeStageHostNames.SourcePermission);

    internal static object CodePermission(ModuleRegistry registry) => Property(registry, SourcePurgeStageHostNames.CodePermission);

    internal static IDisposable SourceScope(ModuleRegistry registry) => (IDisposable)Call(registry, SourcePurgeStageHostNames.EnterSource)!;

    internal static IDisposable SourceTicket(ModuleRegistry registry) => (IDisposable)Call(SourcePermission(registry), SourcePurgeStageHostNames.TryAcquire)!;

    internal static IDisposable ScopeFor(object ticket)
    {
        Type type = typeof(ModuleRegistry).Assembly.GetType(SourcePurgeStageHostNames.ScopeType, true)!;
        ConstructorInfo constructor = type.GetConstructors(SourcePurgeStageHostNames.Members).Single();

        return (IDisposable)Invoke(constructor, null, [ticket, false])!;
    }

    internal static async Task<IDisposable> CodeTicket(ModuleRegistry registry)
    {
        object pending = Call(CodePermission(registry), SourcePurgeStageHostNames.Acquire, CancellationToken.None)!;
        Task task = (Task)pending.GetType().GetMethod(SourcePurgeStageHostNames.AsTask)!.Invoke(pending, null)!;
        await task.WaitAsync(SourcePurgeStageHostNames.Deadline);

        return (IDisposable)Property(task, SourcePurgeStageHostNames.Result);
    }

    internal static bool WithCodeTicket(object ticket, Func<bool> operation)
    {
        MethodInfo method = ticket.GetType().GetMethod(SourcePurgeStageHostNames.TicketRun, SourcePurgeStageHostNames.Members)!.MakeGenericMethod(typeof(bool));

        return (bool)Invoke(method, ticket, [operation])!;
    }

    internal static Task Unwound(ProcessContext context)
        => ((TaskCompletionSource)Property(context, SourcePurgeStageHostNames.Unwound)).Task;

    internal static void Preflight()
    {
        Type registry = typeof(ModuleRegistry);
        _ = registry.GetMethod(SourcePurgeStageHostNames.EnterSource, SourcePurgeStageHostNames.Members)!;
        Type session = registry.GetMethod(SourcePurgeStageHostNames.Prepare, SourcePurgeStageHostNames.Members)!.ReturnType;
        foreach (string name in new[] { SourcePurgeStageHostNames.Image, SourcePurgeStageHostNames.Ticket, SourcePurgeStageHostNames.Generation })
            if (session.GetProperty(name, SourcePurgeStageHostNames.Members) is null)
                throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
    }
}
