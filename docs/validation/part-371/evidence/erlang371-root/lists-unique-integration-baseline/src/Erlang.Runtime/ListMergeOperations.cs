namespace Erlang;

internal static class ListMergeOperations
{
    public static async ValueTask<Term> Merge(
        ProcessContext context,
        Term left,
        Term right,
        bool descending = false,
        bool unique = false,
        Term? function = null,
        Term? position = null
    )
    {
        if (function is not null && function is not FunctionTerm { Arity: 2 })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        int index = position is null ? 0 : ListKeyOperations.Position(position);
        if (left is not (Cons or Nil) || right is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (left is Nil)
            return right;
        if (right is Nil)
            return left;

        ValueTask<bool> Compare(Term first, Term second) => function is not null ? ListCallback.Compare(
            context,
            function,
            first,
            second
        ) : ValueTask.FromResult(TermOrder.Compare(index == 0 ? first : ListKeyOperations.Element(first, index), index == 0 ? second : ListKeyOperations.Element(second, index)) <= 0);
        Term reversed = await MergeReversed(
            left,
            right,
            descending,
            unique,
            Compare
        );

        return ListOperations.Reverse(reversed, Nil.Value);
    }

    public static async ValueTask<Term> MergeReversed(
        Term left,
        Term right,
        bool descending,
        bool unique,
        Func<Term, Term, ValueTask<bool>> compare
    )
    {
        if (unique)
            return descending ? await UniqueReverse(left, right, compare) : await UniqueForward(left, right, compare);
        Term result = Nil.Value;
        while (left is Cons first && right is Cons second)
        {
            bool takeLeft = await compare(first.Head, second.Head) != descending;
            var selected = takeLeft ? first : second;
            result = new Cons(selected.Head, result);
            if (takeLeft)
                left = first.Tail;
            else
                right = second.Tail;
        }
        if (left is not Nil && right is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return ListOperations.Reverse(left is Nil ? right : left, result);
    }

    private static async ValueTask<Term> UniqueForward(Term left, Term right, Func<Term, Term, ValueTask<bool>> compare)
    {
        Term result = Nil.Value;
        Term? lastLeft = null;
        bool scanningLeft = false;
        while (true)
        {
            if (scanningLeft)
            {
                if (right is not Cons heldRight)
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                if (left is Cons first)
                {
                    if (await compare(first.Head, heldRight.Head))
                    {
                        result = new Cons(first.Head, result);
                        lastLeft = first.Head;
                        left = first.Tail;

                        continue;
                    }
                    if (!await compare(heldRight.Head, lastLeft!))
                        result = new Cons(heldRight.Head, result);
                    right = heldRight.Tail;
                    scanningLeft = false;
                }
                else if (left is Nil)
                {
                    if (!await compare(heldRight.Head, lastLeft!))
                        result = new Cons(heldRight.Head, result);

                    return ListOperations.Reverse(heldRight.Tail, result);
                }
                else
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
            }
            else
            {
                if (left is not Cons heldLeft)
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                if (right is Cons second)
                {
                    if (await compare(heldLeft.Head, second.Head))
                    {
                        result = new Cons(heldLeft.Head, result);
                        lastLeft = heldLeft.Head;
                        left = heldLeft.Tail;
                        scanningLeft = true;
                    }
                    else
                    {
                        result = new Cons(second.Head, result);
                        right = second.Tail;
                    }
                }
                else if (right is Nil)
                    return ListOperations.Reverse(left, result);
                else
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
            }
        }
    }

    private static async ValueTask<Term> UniqueReverse(Term left, Term right, Func<Term, Term, ValueTask<bool>> compare)
    {
        Term result = Nil.Value;
        Term? heldRight = null;
        bool scanningRight = false;
        while (true)
        {
            if (!scanningRight)
            {
                if (right is not Cons second)
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                if (left is Cons first)
                {
                    if (await compare(first.Head, second.Head))
                    {
                        heldRight = second.Head;
                        right = second.Tail;
                        scanningRight = true;
                    }
                    else
                    {
                        result = new Cons(first.Head, result);
                        left = first.Tail;
                    }
                }
                else if (left is Nil)
                    return ListOperations.Reverse(right, result);
                else
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
            }
            else
            {
                if (left is not Cons first)
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                if (right is Cons second)
                {
                    if (await compare(first.Head, second.Head))
                    {
                        result = new Cons(heldRight!, result);
                        heldRight = second.Head;
                        right = second.Tail;
                    }
                    else
                    {
                        if (!await compare(heldRight!, first.Head))
                            result = new Cons(heldRight!, result);
                        result = new Cons(first.Head, result);
                        left = first.Tail;
                        scanningRight = false;
                    }
                }
                else if (right is Nil)
                {
                    if (!await compare(heldRight!, first.Head))
                        result = new Cons(heldRight!, result);

                    return ListOperations.Reverse(left, result);
                }
                else
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
            }
        }
    }

    public static async ValueTask<Term> MergeMany(ProcessContext context, Term lists, bool unique)
    {
        Term accumulated = Nil.Value;
        bool descending = false;
        while (true)
        {
            if (lists is Nil)
            {
                if (accumulated is Nil)
                    return Nil.Value;
                lists = accumulated;
                accumulated = Nil.Value;
                descending = !descending;

                continue;
            }
            if (lists is not Cons one)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            if (one.Head is Nil)
            {
                lists = one.Tail;

                continue;
            }
            if (one.Head is not Cons)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            if (one.Tail is Nil)
            {
                if (accumulated is Nil)
                    return descending ? ListOperations.Reverse(one.Head, Nil.Value) : one.Head;
                lists = new Cons(ListOperations.Reverse(one.Head, Nil.Value), accumulated);
                accumulated = Nil.Value;
                descending = !descending;

                continue;
            }
            if (one.Tail is not Cons two)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            if (two.Head is Nil)
            {
                lists = new Cons(one.Head, two.Tail);

                continue;
            }
            if (two.Head is not Cons)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            Term merged;
            if (two.Tail is Cons three && three.Head is Cons)
            {
                merged = await MergeThree(
                    context,
                    descending ? three.Head : one.Head,
                    two.Head,
                    descending ? one.Head : three.Head,
                    descending,
                    unique
                );
                lists = three.Tail;
            }
            else if (two.Tail is Cons empty && empty.Head is Nil)
            {
                lists = new Cons(one.Head, new Cons(two.Head, empty.Tail));

                continue;
            }
            else if (unique || two.Tail is Nil)
            {
                merged = await Merge(
                    context,
                    descending ? two.Head : one.Head,
                    descending ? one.Head : two.Head,
                    descending,
                    unique
                );
                lists = two.Tail;
            }
            else
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            accumulated = new Cons(ListOperations.Reverse(merged, Nil.Value), accumulated);
        }
    }

    public static async ValueTask<Term> MergeThree(
        ProcessContext context,
        Term first,
        Term second,
        Term third,
        bool descending,
        bool unique
    )
    {
        if (first is not (Cons or Nil) || second is not (Cons or Nil) || third is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return await Merge(
            context,
            await Merge(
                context,
                first,
                second,
                descending,
                unique
            ),
            third,
            descending,
            unique
        );
    }
}
