namespace Erlang;

internal static class SourceLoadNifFilename
{
    internal static bool IsValidUtf8(Term value)
    {
        if (value is BitString binary)
            return binary.IsBinary;
        if (value is not Cons and not Nil)
            return false;
        var pending = new Stack<Term>();
        while (true)
        {
            while (value is Cons cell)
            {
                switch (cell.Head)
                {
                    case Integer number:
                        if (number.Value < SourceLoadNifFilenameParameters.MinimumCodePoint
                            || number.Value > SourceLoadNifFilenameParameters.MaximumCodePoint
                            || number.Value >= SourceLoadNifFilenameParameters.FirstSurrogate
                                && number.Value <= SourceLoadNifFilenameParameters.LastSurrogate)
                            return false;
                        break;
                    case Nil:
                        break;
                    case Cons:
                        pending.Push(cell.Tail);
                        value = cell.Head;
                        continue;
                    default:
                        return false;
                }
                value = cell.Tail;
            }
            if (value is not Nil)
                return false;
            if (pending.Count == 0)
                return true;
            value = pending.Pop();
        }
    }
}
