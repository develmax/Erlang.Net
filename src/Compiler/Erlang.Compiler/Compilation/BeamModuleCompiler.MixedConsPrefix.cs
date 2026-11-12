using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ConsLiteralGroup(int Start, int End, int[] Order);

        private sealed record TwoCellConsPrefix(int Input, Term Head, Term[] Keys);

        private int EmitRegisterConsLiteralPrefix(FunctionDefinition function)
        {
            int headRegister = function.Arity;
            int tailRegister = headRegister + 1;
            if (function.Clauses.Count < 2 || tailRegister >= FirstReservedXRegister)
            {
                return 0;
            }

            int input = -1;
            for (int index = 0; index < function.Arity; index++)
            {
                if (function.Clauses[0].Patterns[index] is Pattern.List
                    { Items: [Pattern.Literal { Value: Atom or Integer }], Tail: Erlang.Pattern.Variable })
                {
                    input = index;
                    break;
                }
            }

            if (input < 0)
            {
                return 0;
            }

            var cases = new List<(Term Key, EntryCall? Call, Term? Constant, int Result)>();
            var keys = new HashSet<Term>();
            bool usesTail = false;
            foreach (Clause clause in function.Clauses)
            {
                if (clause.Guard is not null || clause.Patterns[input] is not Pattern.List
                    { Items: [Pattern.Literal { Value: Atom or Integer } head], Tail: Pattern.Variable tail }
                    || cases.Count > 0 && (cases[0].Key is Atom) != (head.Value is Atom)
                    || !keys.Add(head.Value))
                {
                    break;
                }

                var bindings = new string[tailRegister + 1];
                Array.Fill(bindings, VariableScopeNames.Wildcard);
                bindings[tailRegister] = tail.Name;
                for (int index = 0; index < function.Arity; index++)
                {
                    if (index != input)
                    {
                        if (clause.Patterns[index] is not Pattern.Variable argument
                            || argument.Name != VariableScopeNames.Wildcard && bindings.Contains(argument.Name, StringComparer.Ordinal))
                        {
                            return 0;
                        }

                        bindings[index] = argument.Name;
                    }
                }

                EntryCall? call = null;
                Term? constant = null;
                int result = -1;
                if (clause.Body is Expr.Call target)
                {
                    call = ReadEntryCall(target, bindings);
                    if (call is null)
                    {
                        break;
                    }
                }
                else if (clause.Body is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable)
                {
                    result = Array.IndexOf(bindings, variable.Name);
                    if (result < 0)
                    {
                        break;
                    }
                }
                else if (!AttributeLiteral.TryNormalizeConstant(clause.Body, out constant))
                {
                    break;
                }

                usesTail |= result == tailRegister || call?.Call.Arguments.OfType<Expr.Variable>()
                    .Any(argument => argument.Name == tail.Name) == true;
                cases.Add((head.Value, call, constant, result));
            }

            if (cases.Count == 0 || cases.Count == function.Clauses.Count
                || function.Clauses[cases.Count].Patterns[input] is Pattern.List { Items.Count: > 0 })
            {
                return 0;
            }

            int[] labels = cases.Count > 1 ? cases.Select(_ => Label()).ToArray() : [];
            int fallback = Label();
            owner.EmitNonemptyList(input, fallback);
            if (usesTail)
            {
                owner.EmitListParts(input, XRegisterTag, headRegister, XRegisterTag, tailRegister);
            }
            else
            {
                owner.Op(GetHead, XRegisterTag, input);
                owner.Operand(XRegisterTag, headRegister);
            }

            if (labels.Length > 0)
            {
                var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                owner.EmitMembershipSelect(headRegister, fallback, cases.Select((item, index) => (item.Key, labels[index]))
                    .OrderBy(item => item.Key, comparer));
            }
            else
            {
                owner.Op(ExactEqualTest, LabelTag, fallback);
                owner.Operand(XRegisterTag, headRegister);
                owner.LiteralOperand(cases[0].Key);
            }

            for (int index = 0; index < cases.Count; index++)
            {
                if (labels.Length > 0)
                {
                    Mark(labels[index]);
                }

                var (_, call, constant, result) = cases[index];
                if (call is not null)
                {
                    owner.EmitOrderedTailMoves(call.Moves, call.Constants);
                    EmitRegisterTailTarget(call.Call with { Module = call.Module }, call.Call.Arguments.Count);
                }
                else
                {
                    if (constant is not null)
                    {
                        owner.EmitConstant(constant, 0);
                    }
                    else if (result != 0)
                    {
                        owner.MoveRegister(XRegisterTag, result, XRegisterTag, 0);
                    }

                    owner.instructions.WriteByte(Return);
                }
            }

            Mark(fallback);
            return cases.Count;
        }

        private static ConsLiteralGroup[] ReadConsLiteralGroups(FunctionDefinition function, (int Input, int Count) prefix)
        {
            var groups = new List<ConsLiteralGroup>();
            int index = 0;
            while (index < prefix.Count)
            {
                int start = index;
                var values = new Dictionary<int, Term>();
                var keys = new HashSet<Term>();
                while (index < prefix.Count && function.Clauses[index] is { Guard: null } clause
                    && clause.Body is Expr.List { Items: [_], Tail: Expr.Call }
                    && clause.Patterns[prefix.Input] is Pattern.List
                        { Items: [Pattern.Literal { Value: Atom or Integer } literal], Tail: Erlang.Pattern.Variable }
                    && !HasRepeatedConsBindings(clause, prefix.Input) && keys.Add(literal.Value))
                {
                    values.Add(index++, literal.Value);
                }

                if (values.Count > 1)
                {
                    int[] order = values.Keys.OrderByDescending(key => key,
                        Comparer<int>.Create((left, right) => TermOrder.Compare(values[left], values[right], exact: true))).ToArray();
                    groups.Add(new(start, index, order));
                }

                if (index == start)
                {
                    index++;
                }
            }

            return groups.ToArray();
        }

        private static (int Input, int Count)? ReadMixedConsPrefix(FunctionDefinition function)
        {
            for (int input = 0; input < function.Arity; input++)
            {
                int count = 0;
                foreach (Clause clause in function.Clauses)
                {
                    if (EntryShape(clause.Patterns[input]) is not Pattern.List
                        {
                            Items: [var head], Tail: Erlang.Pattern.Variable
                        }
                        || EntryShape(head) is not (Erlang.Pattern.Variable or Erlang.Pattern.Literal or Erlang.Pattern.Tuple))
                    {
                        break;
                    }

                    count++;
                }

                if (count < 2 || count == function.Clauses.Count
                    || function.Clauses.Take(count).Any(clause => clause.Patterns.Where((_, index) => index != input)
                        .Any(pattern => pattern is not Erlang.Pattern.Variable))
                    || function.Clauses.Skip(count).Any(clause => clause.Patterns.Where((_, index) => index != input)
                        .Any(pattern => pattern is not Erlang.Pattern.Variable && ListEntryTest(pattern) != IsNil))
                    || function.Clauses.Skip(count).Any(clause =>
                        clause.Patterns[input] is not (Erlang.Pattern.Variable or Erlang.Pattern.List { Items: [], Tail: null })
                        && clause.Patterns[input] is not Pattern.Literal { Value: Nil }))
                {
                    continue;
                }

                return (input, count);
            }

            return null;
        }

        private static bool CanReuseMixedConsTail(FunctionDefinition function, (int Input, int Count) prefix)
        {
            int input = prefix.Input;

            return function.Clauses.Skip(prefix.Count).All(clause => ListEntryTest(clause.Patterns[input]) == IsNil)
                && function.Clauses[prefix.Count - 1] is { Guard: null } fallback
                && fallback.Patterns[input] is Pattern.List { Items: [Erlang.Pattern.Variable], Tail: Erlang.Pattern.Variable }
                && !HasRepeatedConsBindings(fallback, input)
                && function.Clauses.Take(prefix.Count).All(clause => clause.Patterns[input] is Pattern.List);
        }

        private static TwoCellConsPrefix? ReadTwoCellConsPrefix(FunctionDefinition function)
        {
            if (function.Clauses.Count < 4 || function.Arity + 2 >= FirstReservedXRegister)
            {
                return null;
            }

            for (int input = 0; input < function.Arity; input++)
            {
                if (function.Clauses[0].Patterns[input] is not Pattern.List
                    { Items: [Pattern.Literal head, Erlang.Pattern.Literal], Tail: Erlang.Pattern.Variable }
                    || function.Clauses.Any(clause => clause.Patterns.Where((_, register) => register != input)
                        .Any(pattern => pattern is not Erlang.Pattern.Variable)))
                {
                    continue;
                }

                var keys = new List<Term>();
                var distinct = new HashSet<Term>();
                foreach (Clause clause in function.Clauses)
                {
                    var bindings = new HashSet<string>(StringComparer.Ordinal);
                    if (clause.Guard is not null || clause.Patterns[input] is not Pattern.List
                        { Items: [Pattern.Literal first, Pattern.Literal { Value: Atom or Integer } key], Tail: Erlang.Pattern.Variable }
                        || !head.Value.Equals(first.Value)
                        || keys.Count > 0 && key.Value.GetType() != keys[0].GetType()
                        || clause.Patterns.SelectMany(Semantics.Variables).Any(name => !bindings.Add(name))
                        || !distinct.Add(key.Value))
                    {
                        break;
                    }

                    keys.Add(key.Value);
                }

                int prefixCount = keys.Count + 1;
                if (keys.Count >= 2 && prefixCount < function.Clauses.Count
                    && CanReuseMixedConsTail(function, (input, prefixCount)))
                {
                    return new(input, head.Value, keys.ToArray());
                }
            }

            return null;
        }

        private static FunctionDefinition FoldIdentityNilFallback(FunctionDefinition function, int input)
        {
            if (function.Clauses is not [.., { Guard: null } empty, { Guard: null } fallback]
                || ListEntryTest(empty.Patterns[input]) != BeamEmissionFormat.IsNil
                || !AttributeLiteral.TryNormalizeConstant(empty.Body, out Term value) || value is not Nil
                || !HasUnconditionalParameters(fallback)
                || fallback.Patterns[input] is not Pattern.Variable variable
                || fallback.Body is not Expr.Variable result || variable.Name != result.Name
                || empty.Patterns.Where((_, index) => index != input).Any(pattern => pattern is not Erlang.Pattern.Variable))
            {
                return function;
            }

            return function with { Clauses = [.. function.Clauses.SkipLast(2), fallback] };
        }
    }
}
