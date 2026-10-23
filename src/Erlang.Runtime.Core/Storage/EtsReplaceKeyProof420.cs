namespace Erlang;
// Structural db_match_keeps_key/db_match_eq_body proof; evaluation equality is insufficient.
internal static class EtsReplaceKeyProof420
{
    internal static bool KeepsKey(TupleTerm clause, long position)
    {
        if (clause.Items[2] is not Cons { Tail: Nil } body) return false;
        if (body.Head is Atom { Name: EtsReplaceNames420.Whole }) return true;
        if (clause.Items[0] is not TupleTerm head || position > head.Items.Count || body.Head is not TupleTerm expression) return false;
        bool constant;
        Term output;
        if (expression.Items.Count == 2 && expression.Items[0] is Atom { Name: EtsReplaceNames420.Constant })
        {
            constant = true;
            output = expression.Items[1];
        }
        else if (expression.Items.Count == 1)
        {
            constant = false;
            output = expression.Items[0];
        }
        else return false;
        if (output is not TupleTerm result || position > result.Items.Count) return false;
        Term key = result.Items[(int)position - 1];
        if (Equal(head.Items[(int)position - 1], key, constant)) return true;

        return !constant && key is TupleTerm { Items.Count: 3 } operation
            && operation.Items[0] is Atom { Name: EtsReplaceNames420.Element }
            && operation.Items[1] is Integer number && number.Value == position
            && operation.Items[2] is Atom { Name: EtsReplaceNames420.Whole };
    }

    private static bool Equal(Term pattern, Term expression, bool constant)
    {
        var pending = new Stack<(Term Pattern, Term Expression, bool Constant)>();
        pending.Push((pattern, expression, constant));
        while (pending.TryPop(out var step))
        {
            Term a = step.Pattern, b = step.Expression;
            bool mode = step.Constant;
            if (b is Cons list)
            {
                if (a is not Cons matched) return false;
                pending.Push((matched.Tail, list.Tail, mode));
                pending.Push((matched.Head, list.Head, mode));
                continue;
            }
            if (b is TupleTerm tuple)
            {
                if (!mode)
                {
                    if (tuple.Items.Count == 1 && tuple.Items[0] is TupleTerm constructed) tuple = constructed;
                    else if (tuple.Items.Count == 2 && tuple.Items[0] is Atom { Name: EtsReplaceNames420.Constant })
                    {
                        pending.Push((a, tuple.Items[1], true));
                        continue;
                    }
                    else return false;
                }
                if (a is not TupleTerm matched || matched.Items.Count != tuple.Items.Count) return false;
                for (int i = tuple.Items.Count - 1; i >= 0; i--) pending.Push((matched.Items[i], tuple.Items[i], mode));
                continue;
            }
            // A matched map may contain additional fields, including in const mode.
            if (b is MapTerm) return false;
            if (!a.Equals(b)) return false;
            if (a is Atom { Name: EtsReplaceNames420.Wildcard or EtsReplaceNames420.Bindings or EtsReplaceNames420.Whole }) return false;
            if (mode && MatchSpecCompiler.Variable(a, out _)) return false;
        }

        return true;
    }
}
