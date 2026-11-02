namespace Erlang;

internal static class RuntimeWorkerStartupFailure
{
    internal static Exception Combine(IEnumerable<Exception> failures)
    {
        Exception[] distinct = failures.Distinct<Exception>(ReferenceEqualityComparer.Instance).ToArray();

        return distinct.Length == 1 ? distinct[0] : new AggregateException(distinct);
    }
}
