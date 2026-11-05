namespace Erlang;

public sealed partial class ProcessDictionary
{
    internal Term[] CaptureNodeDistReferences()
    {
        lock (gate) return data.SelectMany(pair => new[] { pair.Key, pair.Value }).ToArray();
    }
}
