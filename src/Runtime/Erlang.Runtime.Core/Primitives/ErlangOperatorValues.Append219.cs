namespace Erlang;

public static partial class ErlangOperatorValues
{
    private static Term AppendSynchronous219(Term left, Term right)
    {
        if (left is Nil)
            return right;

        if (right is not Nil)
            return Cons.From(Cons.Items(left), right);

        // Synchronous public/interpreted value path cannot trap. Preserve its
        // existing budget limitation while validating only the left spine.
        Term cursor = left;
        while (cursor is Cons cell)
            cursor = cell.Tail;
        if (cursor is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return left;
    }
}
