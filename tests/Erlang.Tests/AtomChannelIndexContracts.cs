using Erlang;

namespace Erlang.Tests;

public static class AtomChannelIndexContracts
{
    public static async Task Run()
    {
        string prefix = AtomChannelIndexContractConstants.Prefix + Guid.NewGuid();
        int retained = AtomNameRegistry.Count;
        var indexes = new int[128];
        await Task.WhenAll(Enumerable.Range(0, indexes.Length).Select(worker => Task.Run(() =>
        {
            indexes[worker] = AtomNameRegistry.IndexOf(prefix + worker % 8);
        })));

        if (AtomNameRegistry.IndexOf("false") != 0 || AtomNameRegistry.Count != retained + 8 || indexes.Distinct().Count() != 8)
            throw new InvalidOperationException(AtomChannelIndexContractConstants.RetentionError);
        for (int worker = 0; worker < indexes.Length; worker++)
            if (indexes[worker] != AtomNameRegistry.IndexOf(prefix + worker % 8))
                throw new InvalidOperationException(AtomChannelIndexContractConstants.ChangedIndexError);
    }
}
