using Erlang;
using System.Collections;
using System.Reflection;

namespace Erlang.Tests;

internal static class MapHashRetentionHostAccess
{
    internal static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(MapHashRetentionHostNames.Failure);
    }

    internal static object[] Epochs(ProcessRuntime runtime)
    {
        object known = IdentifierLifetimeHostAccess.Known(runtime);
        var entries = (IDictionary)known.GetType().GetField(
            MapHashRetentionHostNames.Epochs,
            MapHashRetentionHostNames.InstanceFlags
        )!.GetValue(known)!;

        return entries.Values.Cast<object>().ToArray();
    }

    internal static int Active(object epoch)
        => (int)epoch.GetType().GetProperty(
            MapHashRetentionHostNames.ActiveScopes,
            MapHashRetentionHostNames.InstanceFlags
        )!.GetValue(epoch)!;

    internal static object Identity(ProcessRuntime runtime, Term term)
    {
        object? value = IdentifierLifetimeHostAccess.Observed(runtime, term);
        Check(IdentifierLifetimeHostAccess.IsIdentity(value));

        return value!;
    }

    internal static void Closed(Action action)
    {
        try
        {
            action();
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (TargetInvocationException error) when (error.InnerException is ObjectDisposedException)
        {
            return;
        }
        throw new InvalidOperationException(MapHashRetentionHostNames.Failure);
    }

    internal static FunctionTerm Carrier(Term key, Action? invoked = null)
    {
        Term box = Term.Tuple(new MapTerm(new[]
        {
            new KeyValuePair<Term, Term>(Term.A(MapHashRetentionHostNames.EnvironmentKey), Term.List(key))
        }));

        return new FunctionTerm(
            new LocalFunctionIdentity(
                Term.A(MapHashRetentionHostNames.Module),
                null,
                0,
                0,
                new BitString(new byte[MapHashRetentionHostNames.UniqueBytes])
            ),
            0,
            new[] { box },
            (_, _) =>
            {
                invoked?.Invoke();

                return ValueTask.FromResult(box);
            }
        );
    }
}
