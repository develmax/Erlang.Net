using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CatchEntryBranch(
            EntryCall Call,
            int Live,
            Atom CatchTag,
            Term FailureTag,
            Term SuccessTag
        );

        private sealed record CatchEntryPrefix((int Register, Term Value)[] Checks, Term Result, EntryCall? Call = null);

        private bool TryEmitSharedRecordCatchContinuations(FunctionDefinition function)
        {
            const int branchCount = 2;
            const int wrapperArity = 2;
            const int wrapperHeapWords = wrapperArity + 1;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity < 2 || function.Clauses.Count < branchCount + 1
                || function.Clauses[^1] is not
                {
                    Guard: null,
                    Body: Expr.Tuple
                    {
                        Items:
                        [
                            Expr.Literal outerTag,
                            Expr.Tuple { Items: [Expr.Literal innerTag, Expr.Variable failedValue] }
                        ]
                    }
                } fallback
                || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            int input = ArgumentRegister(fallback, failedValue.Name);
            if (input < 0 || failedValue.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            Clause first = function.Clauses[^3];
            Clause second = function.Clauses[^2];
            if (first.Patterns[input] is not Pattern.Tuple
                { Items: [Pattern.Literal { Value: Atom tag }, ..] } firstTuple
                || second.Patterns[input] is not Pattern.Tuple
                { Items: [Pattern.Literal { Value: Atom secondTag }, ..] } secondTuple
                || !tag.Equals(secondTag) || firstTuple.Items.Count != secondTuple.Items.Count)
            {
                return false;
            }

            int[] selectors = Enumerable.Range(1, firstTuple.Items.Count - 1)
                .Where(index => firstTuple.Items[index] is not Erlang.Pattern.Variable).Take(2).ToArray();
            if (selectors is not [int selector])
            {
                return false;
            }

            Term? firstValue = SelectorLiteral(firstTuple.Items[selector]);
            Term? secondValue = SelectorLiteral(secondTuple.Items[selector]);
            bool hasDefault = firstValue is Nil && secondTuple.Items[selector] is Erlang.Pattern.Variable;
            bool hasAtoms = firstValue is Atom && secondValue is Atom && !firstValue.Equals(secondValue);
            if (!hasDefault && !hasAtoms)
            {
                return false;
            }

            int[] fields = new int[firstTuple.Items.Count - 1];
            for (int field = 0; field < fields.Length; field++)
            {
                fields[field] = hasDefault && field == fields.Length - 1 ? input : function.Arity + field;
            }

            if (fields[^1] >= FirstReservedXRegister || fields.Length > FirstReservedXRegister - function.Arity
                || ReadBranch(first, firstTuple) is not { } left || ReadBranch(second, secondTuple) is not { } right
                || !left.CatchTag.Equals(right.CatchTag) || !left.SuccessTag.Equals(right.SuccessTag))
            {
                return false;
            }

            var prefixes = new List<CatchEntryPrefix>();
            foreach (Clause clause in function.Clauses.SkipLast(branchCount + 1))
            {
                if (ReadPrefix(clause) is not { } prefix)
                {
                    return false;
                }

                prefixes.Add(prefix);
            }

            int record = prefixes.Count == 0 ? 0 : Label();
            int badType = Label();
            int success = Label();
            int[] prefixLabels = prefixes.Skip(1).Select(_ => Label()).Append(record).ToArray();
            for (int index = 0; index < prefixes.Count; index++)
            {
                CatchEntryPrefix prefix = prefixes[index];
                for (int check = 0; check < prefix.Checks.Length; check++)
                {
                    var condition = prefix.Checks[check];
                    int next = index;
                    while (check == 1 && next + 1 < prefixes.Count
                        && prefixes[next + 1].Checks is [var selectorCheck, var rejection]
                        && selectorCheck.Register == prefix.Checks[0].Register
                        && !selectorCheck.Value.Equals(prefix.Checks[0].Value)
                        && rejection.Register == condition.Register && rejection.Value.Equals(condition.Value))
                    {
                        next++;
                    }

                    owner.Op(ExactEqualTest, LabelTag, prefixLabels[next]);
                    owner.Operand(XRegisterTag, condition.Register);
                    owner.LiteralOperand(condition.Value);
                }

                if (prefix.Call is { } call)
                {
                    EmitEntryCallWithEmptyFrame(call, []);
                    EmitWrap(prefix.Result, frame: 0);
                }
                else
                {
                    owner.EmitConstant(prefix.Result, 0);
                    owner.instructions.WriteByte(Return);
                }

                Mark(prefixLabels[index]);
            }

            owner.EmitRecordTest(
                badType,
                XRegisterTag,
                input,
                tag,
                firstTuple.Items.Count
            );
            for (int field = 0; field < fields.Length; field++)
            {
                owner.EmitTuplePart(input, field + 1, fields[field]);
            }

            int selected = fields[selector - 1];
            if (hasDefault)
            {
                int other = Label();
                owner.EmitNilTest(XRegisterTag, selected, other);
                EmitBranch(left);
                Mark(other);
                EmitBranch(right);
            }
            else
            {
                int high = Label();
                int low = Label();
                bool leftFirst = TermOrder.Compare(firstValue!, secondValue!, exact: true) < 0;
                owner.Op(SelectValue, XRegisterTag, selected);
                owner.Operand(LabelTag, badType);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 4);
                owner.LiteralOperand(leftFirst ? firstValue! : secondValue!);
                owner.Operand(LabelTag, low);
                owner.LiteralOperand(leftFirst ? secondValue! : firstValue!);
                owner.Operand(LabelTag, high);
                Mark(high);
                EmitBranch(leftFirst ? right : left);
                Mark(low);
                EmitBranch(leftFirst ? left : right);
            }

            Mark(success);
            EmitWrap(left.SuccessTag, frame: 1);
            Mark(badType);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * wrapperHeapWords);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            EmitTaggedValue(innerTag.Value, input);
            EmitTaggedValue(outerTag.Value, 0);
            owner.instructions.WriteByte(Return);

            return true;

            static Term? SelectorLiteral(Pattern pattern) => pattern switch
            {
                Pattern.Literal literal => literal.Value,
                Pattern.List { Items.Count: 0, Tail: null } => Nil.Value,
                _ => null
            };

            CatchEntryBranch? ReadBranch(Clause clause, Pattern.Tuple tuple)
            {
                if (clause.Guard is not null || clause.Body is not Expr.Case
                    {
                        Value: Expr.Catch { Operand: Expr.Call protectedCall },
                        Clauses:
                        [
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple
                            {
                                Items: [Pattern.Literal { Value: Atom catchTag }, Pattern.Variable reason]
                            }],
                            Body: Expr.Tuple { Items: [Expr.Literal error, Expr.Variable rejected] }
                        },
                        {
                            Guard: null,
                            Patterns: [Pattern.Variable result],
                            Body: Expr.Tuple { Items: [Expr.Literal ok, Expr.Variable returned] }
                        }
                        ]
                    }
                    || reason.Name != rejected.Name || result.Name != returned.Name || !DistinctBindings(reason, result)
                    || protectedCall.Arguments.Any(argument => argument is not Expr.Variable))
                {
                    return null;
                }

                var names = Enumerable.Repeat(VariableScopeNames.Wildcard, function.Arity + fields.Length).ToArray();
                var bound = new HashSet<string>(StringComparer.Ordinal);
                for (int register = 0; register < function.Arity; register++)
                {
                    if (register == input)
                    {
                        continue;
                    }

                    if (clause.Patterns[register] is not Pattern.Variable parameter || !Bind(parameter, register))
                    {
                        return null;
                    }
                }

                for (int field = 0; field < fields.Length; field++)
                {
                    Pattern pattern = tuple.Items[field + 1];
                    if (pattern is Pattern.Variable variable)
                    {
                        if (!Bind(variable, fields[field]))
                        {
                            return null;
                        }
                    }
                    else if (field + 1 != selector)
                    {
                        return null;
                    }
                }

                if (bound.Contains(reason.Name) || bound.Contains(result.Name)
                    || ReadEntryCall(protectedCall, names) is not { } call || call.ConstructedArgument is not null
                    || !TryResolveOrdinaryCallTarget(protectedCall, out _))
                {
                    return null;
                }

                return new(
                    call,
                    EntryCallLiveCount(call, names),
                    catchTag,
                    error.Value,
                    ok.Value
                );

                bool Bind(Pattern.Variable variable, int register)
                {
                    if (variable.Name == VariableScopeNames.Wildcard)
                    {
                        return true;
                    }

                    names[register] = variable.Name;

                    return bound.Add(variable.Name);
                }
            }

            CatchEntryPrefix? ReadPrefix(Clause clause)
            {
                if (!HasUnconditionalParameters(clause))
                {
                    return null;
                }

                (int Register, Term Value)?[] checks = clause.Guard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                    ? [ReadCheck(conjunction.Left), ReadCheck(conjunction.Right)] : [ReadCheck(clause.Guard)];
                if (checks.Any(check => check is null)
                    || checks.Length == 2 && checks[0]!.Value.Register == checks[1]!.Value.Register)
                {
                    return null;
                }

                var conditions = checks.Select(check => check!.Value).ToArray();
                if (AttributeLiteral.TryNormalizeConstant(clause.Body, out Term result))
                {
                    return new(conditions, result);
                }

                return clause.Body is Expr.Tuple { Items: [Expr.Literal resultTag, Expr.Call { Arguments.Count: 0 } producer] }
                    && TryResolveOrdinaryCallTarget(producer, out _) && ReadEntryCall(producer, []) is { } call
                    ? new(conditions, resultTag.Value, call) : null;

                (int Register, Term Value)? ReadCheck(Expr? condition)
                {
                    if (condition is not Expr.Binary { Operator: ErlangOperators.ExactEqual } exact)
                    {
                        return null;
                    }

                    if (ReadComparison(exact.Left, exact.Right) is { } direct)
                    {
                        return direct;
                    }

                    return ReadComparison(exact.Right, exact.Left);
                }

                (int Register, Term Value)? ReadComparison(Expr value, Expr constant) =>
                    value is Expr.Variable argument && argument.Name != VariableScopeNames.Wildcard
                    && ArgumentRegister(clause, argument.Name) is int register && register >= 0
                    && AttributeLiteral.TryNormalizeConstant(constant, out Term literal) ? (register, literal) : null;
            }

            void EmitBranch(CatchEntryBranch branch)
            {
                int handler = Label();
                EmitFrameAllocation(branch.Live, 1);
                EnterCatch(0, handler);
                EmitEntryCall(branch.Call);
                EndCatch(0, handler);
                owner.EmitRecordTest(
                    success,
                    XRegisterTag,
                    0,
                    branch.CatchTag,
                    wrapperArity
                );
                EmitWrap(branch.FailureTag, frame: 1, unpack: true);
            }

            void EmitWrap(Term tag, int frame, bool unpack = false)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, wrapperHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                if (unpack)
                {
                    owner.EmitTuplePart(0, 1, 0);
                }

                EmitTaggedValue(tag, 0);
                owner.EmitFrameReturn(frame);
            }

            void EmitTaggedValue(Term tag, int register)
            {
                owner.EmitTupleHeader(XRegisterTag, wrapperArity, 0);
                owner.LiteralOperand(tag);
                owner.Operand(XRegisterTag, register);
            }
        }
    }
}
