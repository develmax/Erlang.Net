namespace Erlang;

internal sealed partial class NativeResourceTypeTable527
{
    // The actual pre-admitted operation owns the publication writer and the
    // target binding gate. Generic publication still validates closing normally.
    internal void CommitAuthorizedUpgrade544(
        List<NativeResourceTypeOpen527> opens,
        int count,
        bool imageRetired
    )
    {
        bool retire = closing || imageRetired;
        for (int index = count - 1; index >= 0; index--)
        {
            NativeResourceTypeOpen527 opened = opens[index];
            if (retire)
                committed.Remove(opened.Type527);
            else if (opened.Created527)
                committed.AddFirst(opened.Node527!);
            opened.Previous527 = opened.Type527.InstallUpgrade544(this, opened.Version527, retire);
        }
    }
}
