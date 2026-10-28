namespace Erlang;

internal static class ProcessSpawnRequestErrorMetadata547
{
    internal static Term? Location(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        string function,
        bool badOption
    )
    {
        RuntimeNativeMetadataImage? image = context.Runtime.NativeMetadataImage527;
        if (image is null || function != ProcessSpawnRequestNames.Function)
            return null;
        Term? options = arguments.Count switch
        {
            2 when arguments[0] is FunctionTerm { Arity: 0 } => arguments[1],
            3 when arguments[0] is Atom && arguments[1] is FunctionTerm { Arity: 0 } => arguments[2],
            4 when arguments[0] is Atom && arguments[1] is Atom && arguments[2] is Cons or Nil => arguments[3],
            5 when arguments[0] is Atom && arguments[1] is Atom && arguments[2] is Atom
                && arguments[3] is Cons or Nil => arguments[4],
            _ => null
        };
        if (options is null)
            return null;
        while (options is Cons cell)
            options = cell.Tail;
        if (options is Nil)
            return null;

        // These are the source wrapper branches for an improper options tail.
        // Other errors and their source partitions deliberately keep their policy.
        return arguments.Count switch
        {
            2 when badOption => image.InheritedErrorLocation545,
            3 when !badOption => image.ErrorInfoLocation545,
            4 when badOption => image.BadargCauseLocation545,
            5 when badOption && arguments[0] is Atom node && node.Name == context.Runtime.Node
                => image.InheritedErrorLocation545,
            _ => null
        };
    }
}
