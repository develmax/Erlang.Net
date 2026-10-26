namespace Erlang;

internal static class NifNativeTermGraph
{
    internal static Term Resolve(NifNativeTermValue value)
    {
        if (value.StaticTerm is { } immediate)
            return immediate;
        var resolved = new Dictionary<NifNativeTermNode, Term>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<(NifNativeTermNode Node, bool Exit)>();
        stack.Push((value.Pending!, false));
        while (stack.TryPop(out var frame))
        {
            if (resolved.ContainsKey(frame.Node))
                continue;
            if (frame.Exit)
            {
                resolved.Add(frame.Node, frame.Node.Build(resolved));
                continue;
            }
            stack.Push((frame.Node, true));
            for (int index = frame.Node.ChildCount - 1; index >= 0; index--)
                if (frame.Node.Child(index).Pending is { } child && !resolved.ContainsKey(child))
                    stack.Push((child, false));
        }

        return resolved[value.Pending!];
    }
}
