using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class BeamNifMarkers410Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BeamNifMarkers410Names.Failure);
    }

    internal static object Property(object target, string name)
        => target.GetType().GetProperty(name, BeamNifMarkers410Names.Members)!.GetValue(target)!;

    internal static object? Call(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, BeamNifMarkers410Names.Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
