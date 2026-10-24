using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal static class UndefinedLambdaPublicationHostReflection
{
    internal static Delegate Factory(ProcessRuntime runtime, FunctionTerm genuine)
    {
        object token = Call(
            runtime.Functions,
            UndefinedLambdaPublicationHostNames.Capture,
            genuine.LocalIdentity!,
            genuine.Arity,
            genuine
        )!;
        object publication = token.GetType().GetProperty(UndefinedLambdaPublicationHostNames.Publication, UndefinedLambdaPublicationHostNames.Members)!.GetValue(token)!;

        return (Delegate)publication.GetType().GetProperty(UndefinedLambdaPublicationHostNames.Factory, UndefinedLambdaPublicationHostNames.Members)!.GetValue(publication)!;
    }

    internal static void Install(ProcessRuntime runtime, FunctionTerm genuine, UndefinedLambdaPublicationHostBarrier barrier)
    {
        MethodInfo method = typeof(RuntimeFunctionEntries).GetMethods(UndefinedLambdaPublicationHostNames.Members)
            .Single(value => value.Name == UndefinedLambdaPublicationHostNames.RegisterSourceFactory);
        Type callback = method.GetParameters()[3].ParameterType;
        Type[] types = callback.GetGenericArguments();
        ParameterExpression environment = Expression.Parameter(types[0]);
        ParameterExpression invocation = Expression.Parameter(types[1]);
        MethodInfo construct = typeof(UndefinedLambdaPublicationHostBarrier).GetMethod(UndefinedLambdaPublicationHostNames.InvokeFactory)!;
        Delegate wrapper = Expression.Lambda(
            callback,
            Expression.Call(
                Expression.Constant(barrier),
                construct,
                environment,
                Expression.Convert(invocation, typeof(object))
            ),
            environment,
            invocation
        ).Compile();
        Invoke(method, runtime.Functions, [genuine.LocalIdentity!, genuine.Arity, genuine.Environment.Count, wrapper]);
    }

    internal static object Gate(object target) => target.GetType().GetField(UndefinedLambdaPublicationHostNames.Gate, UndefinedLambdaPublicationHostNames.Members)!.GetValue(target)!;

    private static object? Call(object target, string name, params object?[] arguments)
        => Invoke(target.GetType().GetMethod(name, UndefinedLambdaPublicationHostNames.Members)!, target, arguments);

    private static object? Invoke(MethodInfo method, object target, object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }
}
