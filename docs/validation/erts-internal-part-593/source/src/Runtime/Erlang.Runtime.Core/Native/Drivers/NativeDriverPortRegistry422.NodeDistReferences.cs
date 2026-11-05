namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal Term[] CaptureNodeDistReferences()
    {
        lock (gate) return ports.Values.Where(port => !port.PhysicallyStopped)
            .Select(port => Term.Tuple(NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Port, port.Identity), port.Data)).ToArray();
    }
}
