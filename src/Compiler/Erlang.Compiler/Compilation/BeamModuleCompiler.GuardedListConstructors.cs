using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ListConstructorBranch(
            Term? Head,
            List<(Term Value, bool Exact)> Guards,
            CallArgumentConstructions Constructor
        );

        private bool TryEmitGuardedListConstructors(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity != 1 || function.Clauses.Count < 3)
            {
                return false;
            }

            IReadOnlyList<Clause> clauses = function.Clauses;
            Term? skipHead = null;
            Expr.Call? skip = null;
            if (clauses[0] is
                {
                    Guard: null,
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Literal literal, Pattern.Variable head], Tail: Pattern.Variable tail
                    }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.List { Items: [Expr.Variable next], Tail: Expr.Variable rest }]
                    } call
                }
                && DistinctBindings(head, tail) && head.Name == next.Name && tail.Name == rest.Name
                && entries.ContainsKey((call.Function, 1)))
            {
                skipHead = literal.Value;
                skip = call;
                clauses = clauses.Skip(1).ToArray();
            }

            if (clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable fallbackHead], Tail: Pattern.Variable fallbackTail }],
                    Body: Expr.Call fallback
                } || !ReadListEntryRegisters(fallbackHead, fallbackTail, out var fallbackRegisters)
                || !TryEmitRegisterConstructedArgument(
                    fallback,
                    fallbackRegisters,
                    validateOnly: true,
                    allowControlTransfer: true
                ))
            {
                return false;
            }

            var branches = new List<ListConstructorBranch>();
            foreach (Clause clause in clauses.SkipLast(1))
            {
                if (clause.Patterns is not [Pattern.List { Items: [var item], Tail: Pattern.Variable tailBinding }])
                {
                    return false;
                }

                Term? key = null;
                List<(Term Value, bool Exact)> guards = [];
                Dictionary<string, int> registers;
                if (item is Pattern.Variable headBinding && clause.Guard is { } guard
                    && headBinding.Name != VariableScopeNames.Wildcard
                    && ReadListEntryRegisters(headBinding, tailBinding, out registers)
                    && ReadHeadEqualityGuards(guard, headBinding.Name) is { Count: > 0 } comparisons)
                {
                    guards = comparisons;
                }
                else if (item is Pattern.Literal headLiteral && clause.Guard is null
                    && ReadListEntryRegisters(new Pattern.Variable(VariableScopeNames.Wildcard), tailBinding, out registers))
                {
                    key = headLiteral.Value;
                }
                else
                {
                    return false;
                }

                if (ReadCallArgumentConstructions(
                    [clause.Body],
                    -1,
                    new Dictionary<string, int>(),
                    inputs: registers
                )
                    is not { Constructions.Count: > 0 } constructor)
                {
                    return false;
                }

                if (key is null && branches.LastOrDefault() is { Head: null } previous
                    && SameListConstructor(previous.Constructor, constructor))
                {
                    previous.Guards.AddRange(guards);
                }
                else
                {
                    branches.Add(new(key, guards, constructor));
                }
            }

            if (!branches.Any(branch => branch.Guards.Count > 1))
            {
                return false;
            }

            owner.EmitNonemptyList(0, functionInfo);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            if (skip is not null)
            {
                int remaining = Label();
                EmitListHeadTest(ExactEqualTest, skipHead!, remaining);
                owner.EmitNonemptyList(0, remaining);
                EmitRegisterTailTarget(skip, 1);
                Mark(remaining);
            }

            foreach (var branch in branches)
            {
                int? success = branch.Guards.Count > 1 ? Label() : null;
                int nextClause = Label();
                if (branch.Head is { } key)
                {
                    EmitListHeadTest(ExactEqualTest, key, nextClause);
                }
                else
                {
                    for (int index = 0; index < branch.Guards.Count; index++)
                    {
                        var (value, exact) = branch.Guards[index];
                        bool last = index == branch.Guards.Count - 1;
                        byte test = (last, exact) switch
                        {
                            (true, true) => ExactEqualTest,
                            (true, false) => NumericEqualTest,
                            (false, true) => ExactNotEqualTest,
                            (false, false) => NumericNotEqualTest
                        };
                        EmitListHeadTest(test, value, last ? nextClause : success!.Value);
                    }
                }

                if (success is int label)
                {
                    Mark(label);
                }

                EmitCallArgumentConstructions(branch.Constructor, -1);
                owner.instructions.WriteByte(Return);
                Mark(nextClause);
            }

            TryEmitRegisterConstructedArgument(fallback, fallbackRegisters, allowControlTransfer: true);

            return true;
        }

        private static bool ReadListEntryRegisters(Pattern.Variable head, Pattern.Variable tail, out Dictionary<string, int> registers)
        {
            registers = new(StringComparer.Ordinal);
            if (head.Name != VariableScopeNames.Wildcard)
            {
                registers.Add(head.Name, 1);
            }

            return tail.Name == VariableScopeNames.Wildcard || registers.TryAdd(tail.Name, 0);
        }

        private static List<(Term Value, bool Exact)>? ReadHeadEqualityGuards(Expr guard, string head)
        {
            IReadOnlyList<Expr> choices = guard is Expr.GuardAlternatives alternatives ? alternatives.Items : [guard];
            var comparisons = new List<(Term Value, bool Exact)>();
            foreach (Expr choice in choices)
            {
                if (choice is not Expr.Binary
                    {
                        Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual, Left: Expr.Variable variable
                    } comparison || variable.Name != head
                    || !AttributeLiteral.TryNormalizeConstant(comparison.Right, out Term value))
                {
                    return null;
                }

                comparisons.Add((value, comparison.Operator == ErlangOperators.ExactEqual));
            }

            return comparisons;
        }

        private static bool SameListConstructor(CallArgumentConstructions first, CallArgumentConstructions second) =>
            first.Live == second.Live && first.Moves.SequenceEqual(second.Moves)
            && first.Loads.SequenceEqual(second.Loads) && first.Constants.SequenceEqual(second.Constants)
            && first.Constructions.Count == second.Constructions.Count
            && first.Constructions.Zip(second.Constructions).All(pair =>
                pair.First.Register == pair.Second.Register && pair.First.IsTuple == pair.Second.IsTuple
                && pair.First.Operands.SequenceEqual(pair.Second.Operands));

        private void EmitListHeadTest(byte opcode, Term value, int failure)
        {
            owner.Op(opcode, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(value);
        }
    }
}
