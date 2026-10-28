namespace Erlang;

internal sealed class OwnedDetachedCopyFrame510
{
    internal Term Source { get; }
    internal Term[] Children { get; }
    internal Term[] Copied { get; }
    internal int Index { get; set; }
    internal NativeMapCopyPlan510? NativeMap { get; }

    internal OwnedDetachedCopyFrame510(ProcessRuntime runtime, Term source)
    {
        Source = source;
        if (source is MapTerm map && map.NativeEquality505 is not null)
        {
            NativeMap = new(runtime, map);
            Children = NativeMap.Children;
        }
        else
            Children = new DebugDirtyCopyFrame424(source).Children;
        Copied = new Term[Children.Length];
    }
}
