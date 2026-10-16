using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Otp;

namespace Erlang.Tests;

public static class ErrorLoggerTermLimitHostChecks
{
    public static Task Run()
    {
        var first = Term.A(ErrorLoggerTermLimitHostNames.First);
        var second = Term.A(ErrorLoggerTermLimitHostNames.Second);
        Term[] fitting = [
            Term.Tuple(first, second),
            Term.List(first, second),
            new MapTerm([new(first, Term.Tuple(first, second))]),
            Term.String(ErrorLoggerTermLimitHostNames.First),
            Term.Tuple(),
            new BitString([0, 1])
        ];
        foreach (Term value in fitting)
            if (!ReferenceEquals(value, Limit(value, ErrorLoggerTermLimitHostNames.FormatDepth)))
                throw new InvalidOperationException(ErrorLoggerTermLimitHostNames.Missing);
        Term nested = first;
        for (int index = 0; index < ErrorLoggerTermLimitHostNames.NestedLevels; index++)
            nested = Term.Tuple(nested);
        if (!ReferenceEquals(nested, Limit(nested, ErrorLoggerTermLimitHostNames.SufficientDepth)))
            throw new InvalidOperationException(ErrorLoggerTermLimitHostNames.Missing);

        // test_limit conservatively fails even when limit reconstructs an equal map.
        var conservative = new MapTerm(Enumerable.Range(0, ErrorLoggerTermLimitHostNames.FormatDepth)
            .Select(index => new KeyValuePair<Term, Term>(Term.I(index), first)));
        Term rebuilt = Limit(conservative, ErrorLoggerTermLimitHostNames.FormatDepth);
        if (ReferenceEquals(conservative, rebuilt) || !conservative.Equals(rebuilt))
            throw new InvalidOperationException(ErrorLoggerTermLimitHostNames.Missing);
        Term deepKey = first;
        for (int index = 0; index < ErrorLoggerTermLimitHostNames.DeepKeyLevels; index++)
            deepKey = Term.Tuple(deepKey);
        var keyed = new MapTerm([new(deepKey, second)]);
        Term keyedResult = Limit(keyed, ErrorLoggerTermLimitHostNames.FormatDepth);
        if (ReferenceEquals(keyed, keyedResult) || !keyed.Equals(keyedResult))
            throw new InvalidOperationException(ErrorLoggerTermLimitHostNames.Missing);

        var large = new MapTerm(Enumerable.Range(0, ErrorLoggerTermLimitHostNames.LargeMapSize)
            .Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.Tuple(first, Term.I(index)))));
        var expectedEntries = new List<KeyValuePair<Term, Term>>();
        Term iterator = MapIteration.Iterator(large);
        for (int index = 0; index <= ErrorLoggerTermLimitHostNames.FormatDepth; index++)
        {
            if (MapIteration.Next(iterator) is not TupleTerm { Items: [var key, var value, var next] })
                throw new InvalidOperationException(ErrorLoggerTermLimitHostNames.Missing);
            expectedEntries.Add(new(key, value));
            iterator = next;
        }
        if (!Limit(large, ErrorLoggerTermLimitHostNames.FormatDepth).Equals(new MapTerm(expectedEntries)))
            throw new InvalidOperationException(ErrorLoggerTermLimitHostNames.Missing);

        return Task.CompletedTask;
    }

    private static Term Limit(Term value, int depth)
    {
        MethodInfo method = typeof(GenServer).Assembly.GetType(ErrorLoggerTermLimitHostNames.Type, true)!
            .GetMethod(ErrorLoggerTermLimitHostNames.Method)!;
        try
        {
            return (Term)method.Invoke(null, [value, Term.I(depth)])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
    }
}
