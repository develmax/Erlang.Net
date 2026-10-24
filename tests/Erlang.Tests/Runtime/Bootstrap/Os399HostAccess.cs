using Erlang;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class Os399HostAccess
{
    internal static Type Type(string name) => typeof(ProcessRuntime).Assembly.GetType(Os399HostNames.Prefix + name, true)!;

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Os399HostNames.Invariant);
    }

    internal static object? Call(
        object? target,
        string type,
        string name,
        params object?[] args
    )
    {
        MethodInfo method = Type(type).GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == name && m.GetParameters().Length == args.Length);
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
    }

    internal static Term Text(string value) => Cons.From(value.Select(c => (Term)Term.I(c)));

    internal static object Store() => Activator.CreateInstance(
        Type(Os399HostNames.Store),
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new object[] { Array.Empty<KeyValuePair<string, string>>() },
        null
    )!;

    internal static Term StoreCall(object store, string method, params object?[] args) => (Term)Call(
        store,
        Os399HostNames.Store,
        method,
        args
    )!;

    internal static ErlangFunction? Capture(
        ProcessRuntime? runtime,
        string module,
        string function,
        int arity
    )
    {
        object?[] args = [runtime, module, function, arity, null];
        bool admitted = (bool)Type(Os399HostNames.Native).GetMethod("TryCapture", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!;
        Require(admitted == (args[4] is ErlangFunction));

        return (ErlangFunction?)args[4];
    }
}
