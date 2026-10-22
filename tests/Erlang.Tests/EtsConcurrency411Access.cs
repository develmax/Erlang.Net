using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal static class EtsConcurrency411Access
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    internal static object Parse(Term options, CancellationToken cancellation = default)
    {
        try
        {
            return typeof(ProcessRuntime).Assembly.GetType(EtsConcurrency411Names.OptionsType, true)!
                .GetMethod(EtsConcurrency411Names.Parse, Members)!.Invoke(null, [options, cancellation])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static object Read(object options, string name) => options.GetType().GetProperty(name, Members)!.GetValue(options)!;

    internal static void Refuse(Term options, string kind)
    {
        bool refused = false;
        try
        {
            _ = Parse(options);
        }
        catch (Exception error)
        {
            refused = error.GetType().Name == kind;
        }
        EtsFoundation405Access.Require(refused);
    }
}
