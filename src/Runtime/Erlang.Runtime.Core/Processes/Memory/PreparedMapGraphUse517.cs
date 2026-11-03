namespace Erlang;

// Acquire/dispose this outside every target heap gate. A plan may close while
// an admitted build finishes; its real index reader cannot retire prematurely.
internal sealed class PreparedMapGraphUse517 : IDisposable
{
    private readonly Dictionary<PreparedOwnedMap517, PreparedOwnedMapUse517> uses = new(ReferenceEqualityComparer.Instance);

    internal static PreparedMapGraphUse517 Capture(IEnumerable<PreparedOwnedMap517> plans)
    {
        var result = new PreparedMapGraphUse517();
        try
        {
            foreach (var plan in plans)
            {
                var use = plan.EnterUse517();
                try
                {
                    result.uses.Add(plan, use);
                }
                catch (Exception original)
                {
                    try
                    {
                        use.Dispose();
                    }
                    catch (Exception cleanup)
                    {
                        throw new AggregateException(original, cleanup);
                    }
                    throw;
                }
            }

            return result;
        }
        catch (Exception original)
        {
            try
            {
                result.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    internal PreparedOwnedMapUse517 For(PreparedOwnedMap517 plan) => uses.TryGetValue(plan, out var use)
        ? use : throw new InvalidOperationException(OwnedMapPayloadNames517.PreparationRequired);

    public void Dispose()
    {
        List<Exception>? errors = null;
        foreach (var use in uses.Values)
            try
            {
                use.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(error);
            }
        uses.Clear();
        if (errors is not null)
            throw new AggregateException(errors);
    }
}
