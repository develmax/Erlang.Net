using System.Reflection;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResourceArena528Names;

namespace Erlang.Tests;

internal static class NativeResourceArena528Access
{
    internal static object Owner(ProcessRuntime runtime)
    {
        object owner = NativeResource527Access.New(AllocationOwner);
        Call(owner, "JoinDomain", Property(runtime, Arena));
        Call(owner, "AttachImage528");

        return owner;
    }

    internal static object ProviderOwner(ProcessRuntime runtime, object provider)
    {
        object owner = Property(provider, AllocationProvider);
        Call(owner, "JoinDomain", Property(runtime, Arena));
        Call(owner, "AttachImage528");

        return owner;
    }

    internal static object? Static(string typeName, string method, params object?[] args)
    {
        try
        {
            return typeof(ProcessRuntime).Assembly.GetType(typeName, true)!
                .GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static void Free(nint pointer) => Static(AllocationOwner, "FreeFromCurrentScope528", pointer);

    internal static IDisposable Scope(object owner)
        => (IDisposable)NativeResource527Access.New(
            CallbackScope,
            owner,
            NativeResource527Access.New(FailureState),
            true
        );

    internal static void Close(object owner) => Call(owner, "ImageClosed528");
}
