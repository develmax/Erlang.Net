using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedPlainConsGuards(FunctionDefinition function, int functionInfo)
        {
            const int leftInput = 0;
            const int rightInput = 1;
            const int leftHead = 2;
            const int leftTail = 3;
            const int rightHead = 4;
            const int rightTail = 5;
            const int consBranches = 3;
            if (function.Arity != 2 || function.Clauses.Count != consBranches + 2)
            {
                return false;
            }

            IReadOnlyList<Clause> clauses = function.Clauses;
            var bindings = new string[consBranches][];
            var live = new HashSet<int>();
            for (int branch = 0; branch < bindings.Length; branch++)
            {
                Clause clause = clauses[branch];
                string[] inputs = Enumerable.Repeat(VariableScopeNames.Wildcard, rightTail + 1).ToArray();
                var names = new HashSet<string>(StringComparer.Ordinal);
                for (int column = 0; column < function.Arity; column++)
                {
                    Pattern pattern = clause.Patterns[column];
                    if (pattern is AliasPattern alias)
                    {
                        if (!TryReadPatternAlias(alias, out var variable, out pattern) || !Bind(column, variable))
                        {
                            return false;
                        }
                    }

                    if (pattern is not Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }
                        || !Bind(column == leftInput ? leftHead : rightHead, head)
                        || !Bind(column == leftInput ? leftTail : rightTail, tail))
                    {
                        return false;
                    }
                }

                if (branch < bindings.Length - 1)
                {
                    if (clause.Guard is not Expr.Binary
                        {
                            Operator: ErlangOperators.Less or ErlangOperators.Greater,
                            Left: Expr.Variable left, Right: Expr.Variable right
                        }
                        || Array.IndexOf(inputs, left.Name) is not (leftHead or rightHead)
                        || Array.IndexOf(inputs, right.Name) is not (leftHead or rightHead)
                        || left.Name == right.Name)
                    {
                        return false;
                    }
                }
                else if (clause.Guard is not null)
                {
                    return false;
                }

                foreach (Expr expression in LocalFunctionEntries.Walk(clause.Body))
                {
                    if (expression is Expr.Variable variable && Array.IndexOf(inputs, variable.Name) is int register
                        && register >= 0)
                    {
                        live.Add(register);
                    }
                }

                bindings[branch] = inputs;

                bool Bind(int register, Pattern.Variable variable)
                {
                    if (variable.Name != VariableScopeNames.Wildcard && !names.Add(variable.Name))
                    {
                        return false;
                    }

                    inputs[register] = variable.Name;

                    return true;
                }
            }

            // Reserve a contiguous X-register prefix before entering branch-owned frames.
            // Entries that do not use both tails need a different register assignment.
            if (!live.Contains(leftTail) || !live.Contains(rightTail)
                || clauses[consBranches] is not { Guard: null, Patterns: [var firstNil, Pattern.Variable firstOther] } firstCold
                || clauses[consBranches + 1] is not { Guard: null, Patterns: [Pattern.Variable secondOther, var secondNil] } secondCold
                || ListEntryTest(firstNil) != IsNil || ListEntryTest(secondNil) != IsNil
                || !TryReadReturn(firstCold.Body, [VariableScopeNames.Wildcard, firstOther.Name], out var firstReturn)
                || !TryReadReturn(secondCold.Body, [secondOther.Name, VariableScopeNames.Wildcard], out var secondReturn))
            {
                return false;
            }

            bool reuseSecondInput = !live.Contains(rightInput);
            var arms = new (EntryCall? Tail, ConsCallResult? Cons, (int Register, Term? Value) Result,
                (byte Opcode, int Left, int Right)? Test)[bindings.Length];
            for (int branch = 0; branch < arms.Length; branch++)
            {
                string[] inputs = bindings[branch];
                if (reuseSecondInput)
                {
                    inputs[rightInput] = inputs[rightTail];
                    inputs[rightTail] = VariableScopeNames.Wildcard;
                }

                Clause clause = clauses[branch];
                EntryCall? tail = clause.Body is Expr.Call call
                    ? ReadEntryCall(call, inputs, allowControlTransfer: true) : null;
                ConsCallResult? cons = tail is null ? ReadConsCallResult(clause.Body, inputs) : null;
                if (cons is { HeadCall: not null } or { Tuple: not null })
                {
                    return false;
                }

                (int Register, Term? Value) result = default;
                if (tail is null && cons is null && !TryReadReturn(clause.Body, inputs, out result))
                {
                    return false;
                }

                (byte Opcode, int Left, int Right)? test = null;
                if (clause.Guard is Expr.Binary { Left: Expr.Variable left, Right: Expr.Variable right } comparison)
                {
                    bool reverse = comparison.Operator == ErlangOperators.Greater;
                    int first = Array.IndexOf(inputs, left.Name);
                    int second = Array.IndexOf(inputs, right.Name);
                    test = (IsLess, reverse ? second : first, reverse ? first : second);
                }

                arms[branch] = (tail, cons, result, test);
            }

            bool mergeFirstGuard = arms[0] is { Tail: null, Cons: null, Result.Register: -1 }
                && secondReturn.Register == -1 && arms[0].Result.Value!.Equals(secondReturn.Value);
            if (mergeFirstGuard && arms[1].Test is { } secondTest && arms[0].Test is { } firstTest
                && (secondTest.Left != firstTest.Right || secondTest.Right != firstTest.Left))
            {
                mergeFirstGuard = false;
            }

            int secondGuard = mergeFirstGuard ? 0 : Label();
            int unguarded = Label();
            int firstNonCons = Label();
            int secondNonCons = Label();
            int secondResult = mergeFirstGuard || SameColdReturn() ? Label() : 0;
            // Constructor and guard failures have separate continuations.
            // Branches following the first successful cons checks need only the guard tests.
            EmitColumn(
                leftInput,
                leftHead,
                leftTail,
                firstNonCons
            );
            EmitColumn(
                rightInput,
                rightHead,
                reuseSecondInput ? rightInput : rightTail,
                secondNonCons
            );

            for (int branch = 0; branch < arms.Length; branch++)
            {
                var arm = arms[branch];
                if (branch == 1 && !mergeFirstGuard)
                {
                    Mark(secondGuard);
                }
                else if (branch == 2)
                {
                    Mark(unguarded);
                }

                if (arm.Test is { } test)
                {
                    bool invert = branch == 0 && mergeFirstGuard;
                    owner.Op(invert ? IsGreaterOrEqual : test.Opcode, LabelTag,
                        invert ? secondResult : branch == 0 ? secondGuard : unguarded);
                    owner.Operand(XRegisterTag, test.Left);
                    owner.Operand(XRegisterTag, test.Right);
                    if (invert)
                    {
                        continue;
                    }
                }

                if (arm.Tail is { } tail)
                {
                    EmitRegisterTailCall(tail);
                }
                else if (arm.Cons is { } cons)
                {
                    int callLive = EntryCallLiveCount(cons.Call, bindings[branch]);
                    EmitConsCallResult(cons, Math.Max(callLive, cons.Saved.Select(register => register + 1).DefaultIfEmpty().Max()));
                }
                else
                {
                    EmitReturn(arm.Result);
                }
            }

            Mark(firstNonCons);
            if (SameColdReturn())
            {
                owner.Op(ExactNotEqualTest, LabelTag, secondResult);
                owner.Operand(XRegisterTag, leftInput);
                owner.LiteralOperand(Nil.Value);
            }
            else
            {
                owner.EmitNilTest(XRegisterTag, leftInput, secondNonCons);
                EmitReturn(firstReturn == (-1, Nil.Value) ? (0, null) : firstReturn);
            }

            Mark(secondNonCons);
            owner.EmitNilTest(XRegisterTag, rightInput, functionInfo);
            if (secondResult != 0)
            {
                Mark(secondResult);
            }

            EmitReturn(secondReturn);

            return true;

            bool SameColdReturn() => firstReturn == (-1, Nil.Value)
                ? secondReturn.Register == leftInput
                : firstReturn == secondReturn;

            void EmitColumn(
                int input,
                int head,
                int tail,
                int failure
            )
            {
                owner.EmitNonemptyList(input, failure);
                owner.EmitListParts(
                    input,
                    XRegisterTag,
                    head,
                    XRegisterTag,
                    tail
                );
            }

            void EmitReturn((int Register, Term? Value) result)
            {
                if (result.Register >= 0)
                {
                    if (result.Register != 0)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            result.Register,
                            XRegisterTag,
                            0
                        );
                    }
                }
                else
                {
                    owner.EmitConstant(result.Value!, 0);
                }

                owner.instructions.WriteByte(Return);
            }

            static bool TryReadReturn(Expr body, string[] inputs, out (int Register, Term? Value) result)
            {
                if (body is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                    && Array.IndexOf(inputs, variable.Name) is int register && register >= 0)
                {
                    result = (register, null);

                    return true;
                }

                bool constant = AttributeLiteral.TryNormalizeConstant(body, out Term value);
                result = (-1, value);

                return constant;
            }
        }
    }
}
