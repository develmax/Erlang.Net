using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.ProcessPayloadCore641Names;

namespace Erlang.Tests;

internal static class ProcessPayloadCore641Access
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static object? Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value);

    internal static object Property(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    internal static object? Call(object value, string name, params object?[] arguments)
        => Invoke(value.GetType().GetMethod(name, Members)!, value, arguments);

    internal static object Static(string name, params object?[] arguments)
        => Invoke(typeof(ProcessRuntime).Assembly.GetType(PayloadType, true)!.GetMethod(name, Members)!, null, arguments)!;

    private static object? Invoke(MethodInfo method, object? value, object?[] arguments)
    {
        try
        {
            return method.Invoke(value, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    internal static bool OnlyExpected(Exception error, Exception expected)
        => ReferenceEquals(error, expected)
            || error is AggregateException aggregate && aggregate.InnerExceptions.Count != 0
                && aggregate.InnerExceptions.All(item => OnlyExpected(item, expected));

    internal static async Task Fault(Task operation, Exception expected)
    {
        bool seen = false;
        try
        {
            await operation.WaitAsync(Deadline);
        }
        catch (Exception error) when (OnlyExpected(error, expected))
        {
            seen = true;
        }
        Require(seen);
    }

    internal static void Refused(Action action)
    {
        bool refused = false;
        try
        {
            action();
        }
        catch (ObjectDisposedException)
        {
            refused = true;
        }
        Require(refused);
    }
}
