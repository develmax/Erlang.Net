namespace Erlang;

internal sealed partial class RuntimePorts
{
    internal Term[] CaptureNodeDistReferences()
    {
        lock (gate) return ports.Values.Where(port => !port.Closed)
            .Select(port => Term.Tuple(NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Port, port.Identity), port.Data)).ToArray();
    }
}
