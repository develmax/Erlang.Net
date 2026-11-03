using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.EtsDispatch638Names;

namespace Erlang.Tests;

internal static class EtsDispatch638Access
{
    internal static void Install(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsCompiled419.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsFixation420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsCursor420.Register(runtime, runtime.Modules);
    }

    internal static (object Native, Term Reference) PublishNative(ProcessContext context)
    {
        Term specification = Term.List(Term.Tuple(Term.A(MatchSpecNames.Wildcard), Nil.Value,
            Term.List(Term.Tuple(Term.A(MatchSpecNames.Constant), Term.I(Value)))));
        object?[] args = [context, specification, MatchSpecDialect.Table, null];
        Type compiler = typeof(ProcessRuntime).Assembly.GetType(CompilerType, true)!;
        Require((bool)Invoke(compiler.GetMethod(Compile, Members)!, null, args)!);
        object native = args[3] ?? throw new InvalidOperationException(Failure);
        try
        {
            object payload = Wrap(native);
            Term reference = (Term)Call(
                context.Runtime,
                Publish,
                context,
                payload
            )!;

            return (native, reference);
        }
        catch (Exception error)
        {
            try
            {
                ((Task)Call(native, Close)!).GetAwaiter().GetResult();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(error, cleanup);
            }
            throw;
        }
    }

    internal static object Wrap(object native)
    {
        try
        {
            return Activator.CreateInstance(
                typeof(ProcessRuntime).Assembly.GetType(PayloadType, true)!,
                Members,
                null,
                [native],
                null
            )!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static object? Call(object owner, string name, params object?[] args)
        => Invoke(owner.GetType().GetMethod(name, Members)!, owner, args);

    private static object? Invoke(MethodInfo method, object? owner, object?[] args)
    {
        try
        {
            return method.Invoke(owner, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static bool PhysicalFreed(object native)
        => (bool)native.GetType().GetProperty(Freed, Members)!.GetValue(native)!;

    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
