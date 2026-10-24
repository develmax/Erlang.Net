using System.Reflection;

namespace Erlang.Tests;

internal static class GarbageCollectionResultHostAccess
{
    private static readonly Func<ProcessContext, Term, int, ValueTask<Term>> Gather = Bind();

    internal static ValueTask<Term> GatherAsync(
        ProcessContext context,
        Term reference,
        int expectedReplies
    ) => Gather(context, reference, expectedReplies);

    private static Func<ProcessContext, Term, int, ValueTask<Term>> Bind()
    {
        Type type = typeof(ProcessContext).Assembly.GetType(GarbageCollectionResultHostAccessNames.Type, true)!;
        MethodInfo? method = type.GetMethod(
            GarbageCollectionResultHostAccessNames.Method,
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(ProcessContext), typeof(Term), typeof(int)],
            null
        );
        if (method is null || method.ReturnType != typeof(ValueTask<Term>))
            throw new InvalidOperationException(GarbageCollectionResultHostAccessNames.SignatureFailure);

        return method.CreateDelegate<Func<ProcessContext, Term, int, ValueTask<Term>>>();
    }
}
