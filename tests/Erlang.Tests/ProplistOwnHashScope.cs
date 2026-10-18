using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal sealed class ProplistOwnHashScope : IDisposable
{
    private const BindingFlags InstanceHidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
    private readonly object scope;
    private readonly MethodInfo hash;

    internal ProplistOwnHashScope(ProcessRuntime runtime)
    {
        MethodInfo factory = typeof(ProcessRuntime).GetMethods(InstanceHidden)
            .SingleOrDefault(method => method.Name == ProplistOwnHashNames.FactoryName && method.GetParameters().Length == 1)
            ?? throw new InvalidOperationException(ProplistOwnCursorDiagnostics.MissingScope);
        MethodInfo append = typeof(ProcessRuntime).Assembly.GetType(ProplistOwnHashNames.LayoutType)?.GetMethod(ProplistOwnHashNames.LayoutMethod, StaticHidden)
            ?? throw new InvalidOperationException(ProplistOwnCursorDiagnostics.MissingScope);
        Delegate callback = append.CreateDelegate(factory.GetParameters()[0].ParameterType);
        scope = factory.Invoke(runtime, [callback])!;
        hash = scope.GetType().GetMethod(ProplistOwnHashNames.HashMethod, InstanceHidden)
            ?? throw new InvalidOperationException(ProplistOwnCursorDiagnostics.MissingHash);
    }

    internal ulong Hash(Term term)
    {
        try
        {
            return (ulong)hash.Invoke(scope, [term])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    public void Dispose() => ((IDisposable)scope).Dispose();
}
