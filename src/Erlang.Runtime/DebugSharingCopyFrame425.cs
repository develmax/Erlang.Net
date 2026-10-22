namespace Erlang;

internal sealed class DebugSharingCopyFrame425
{
    internal DebugSharingCopyFrame425(Term source)
    {
        Source = source;
        Children = DebugSharingGraph425.Children(source);
        Copied = new Term[Children.Length];
    }

    internal Term Source { get; }
    internal Term[] Children { get; }
    internal Term[] Copied { get; }
    internal int Index { get; set; }
}
