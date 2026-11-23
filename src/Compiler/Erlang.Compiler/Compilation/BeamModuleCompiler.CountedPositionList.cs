using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string SortListFunction = "sort";

        private bool TryEmitCountedPositionList(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, 3))
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [
                            Pattern.Literal { Value: Integer zero },
                            Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable { }
                        ],
                    Body: { } empty
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Integer done }, Pattern.Variable ignored, Pattern.Variable unused],
                    Body: Expr.Call
                    {
                        Function: AutoImportNames.Exit, DynamicRemote: false, Arguments: [{ } reason]
                    } exit
                },
                { } selected,
                { } skipped,
                { } exhausted
                ]
                || !zero.Value.IsZero || !done.Value.IsZero
                || ignored.Name != VariableScopeNames.Wildcard && ignored.Name == unused.Name
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyValue) || emptyValue is not Nil
                || !owner.ResolvesErlangBif(exit) || !AttributeLiteral.TryNormalizeConstant(reason, out Term error)
                || ReadStep(selected, list: true, matched: true) is not { } onSelected
                || ReadStep(skipped, list: true, matched: false) is not { } onSkipped
                || ReadStep(exhausted, list: false, matched: false) is not { } onExhausted
                || !onSkipped.Value.Equals(onExhausted.Value)
                || !entries.TryGetValue((function.Name, 3), out int entry))
            {
                return false;
            }

            bool entryWitnessSeen = false;
            if (!HasOnlyLocalCalls(
                function,
                3,
                (caller, call) =>
                {
                    if (ReferenceEquals(caller, function))
                    {
                        return ReferenceEquals(call, onSelected.Call) || ReferenceEquals(call, onSkipped.Call)
                            || ReferenceEquals(call, onExhausted.Call);
                    }

                    entryWitnessSeen = true;

                    return ResolveBinding(caller, call, call.Arguments[0]) is
                        (Expr.Binary { Operator: ErlangOperators.Plus or ErlangOperators.Minus } or Expr.Literal { Value: Integer or FloatTerm })
                        && ProvesProperListReturn(ResolveBinding(caller, call, call.Arguments[1]), [])
                        && ResolveBinding(caller, call, call.Arguments[2]) is Expr.Literal { Value: Integer initial }
                        && initial.Value.IsZero;
                }
            ) || !entryWitnessSeen)
            {
                return false;
            }

            int rejected = Label();
            int nonzero = Label();
            int skip = Label();
            int emptyPositions = Label();
            owner.Op(ExactEqualTest, LabelTag, nonzero);
            owner.EmitTypedRegister(0, NumberTypeBits);
            owner.LiteralOperand(zero);
            owner.Op(IsNil, LabelTag, rejected);
            owner.EmitTypedRegister(1, ProperListTypeBits);
            owner.EmitConstant(Nil.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(rejected);
            owner.EmitConstant(error, 0);
            owner.EmitCallLine(exit, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, exit.Function, 1));

            Mark(nonzero);
            owner.Op(IsNonemptyList, LabelTag, emptyPositions);
            owner.Operand(XRegisterTag, 1);
            owner.Op(GetList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 4);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.EmitTypedRegister(2, IntegerTypeBits, lower: 0);
            owner.Operand(XRegisterTag, 3);
            EmitStep(
                onSelected,
                live: 5,
                position: 3,
                output: 2
            );
            EmitFrameAllocation(5, 0);
            owner.MoveRegister(
                XRegisterTag,
                4,
                XRegisterTag,
                1
            );
            EmitResult(onSelected);

            Mark(skip);
            EmitStep(
                onSkipped,
                live: 3,
                position: 2,
                output: 2
            );
            EmitFrameAllocation(3, 0);
            EmitResult(onSkipped);

            Mark(emptyPositions);
            EmitStep(
                onExhausted,
                live: 3,
                position: 2,
                output: 1
            );
            EmitFrameAllocation(2, 0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitConstant(Nil.Value, 1);
            EmitResult(onExhausted);

            return true;

            (Expr.Call Call, Expr.Binary Decrement, Expr.Binary Increment, Term Value)? ReadStep(
                Clause branch,
                bool list,
                bool matched
            )
            {
                if (branch is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable size, var positions, Pattern.Variable index],
                        Body: Expr.List
                        {
                            Items: [Expr.Literal { Value: Integer prepend }],
                            Tail: Expr.Call
                            {
                                Module: null, DynamicRemote: false,
                                Arguments: [
                                    Expr.Binary
                                {
                                    Operator: ErlangOperators.Minus, Left: Expr.Variable count,
                                    Right: Expr.Literal { Value: Integer decrement }
                                } decreased,
                                    var remaining,
                                    Expr.Binary
                                {
                                    Operator: ErlangOperators.Plus, Left: Expr.Variable position,
                                    Right: Expr.Literal { Value: Integer increment }
                                } increased
                                ]
                            } recur
                        }
                    }
                    || !DistinctBindings(size, index) || count.Name != size.Name || position.Name != index.Name
                    || !decrement.Value.IsOne || !increment.Value.IsOne || recur.Function != function.Name)
                {
                    return null;
                }

                if (list)
                {
                    if (positions is not Pattern.List { Items: [Pattern.Variable next], Tail: Pattern.Variable rest }
                        || !DistinctBindings(size, next, rest) || rest.Name == index.Name
                        || matched != (next.Name == index.Name)
                        || (matched
                            ? remaining is not Expr.Variable tail || tail.Name != rest.Name
                            : remaining is not Expr.List { Items: [Expr.Variable head], Tail: Expr.Variable oldTail }
                                || head.Name != next.Name || oldTail.Name != rest.Name))
                    {
                        return null;
                    }
                }
                else if (positions is not (Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil })
                    || !AttributeLiteral.TryNormalizeConstant(remaining, out Term noPositions) || noPositions is not Nil)
                {
                    return null;
                }

                return (recur, decreased, increased, prepend);
            }

            void EmitStep(
                (Expr.Call Call, Expr.Binary Decrement, Expr.Binary Increment, Term Value) step,
                int live,
                int position,
                int output
            )
            {
                var previous = location;
                owner.EmitArithmeticLine(step.Decrement.SourceLine, ref previous);
                owner.GcArithmetic(
                    GcBifTwo,
                    step.Decrement.Operator,
                    live,
                    0,
                    (XRegisterTag, 0, NumberTypeBits),
                    (BeamWire.IntegerTag, 1, 0)
                );
                owner.EmitArithmeticLine(step.Increment.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, step.Increment.Operator, 2));
                owner.EmitTypedRegister(position, IntegerTypeBits, lower: 0);
                owner.LiteralOperand(((Expr.Literal)step.Increment.Right).Value);
                owner.Operand(XRegisterTag, output);
            }

            void EmitResult((Expr.Call Call, Expr.Binary Decrement, Expr.Binary Increment, Term Value) step)
            {
                owner.EmitCallLine(step.Call, location);
                owner.EmitFieldCall(3, entry);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(step.Value);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(0);
            }
        }

        private Expr ResolveBinding(FunctionDefinition caller, Expr.Call consumer, Expr expression)
        {
            if (expression is not Expr.Variable variable)
            {
                return expression;
            }

            foreach (Clause clause in caller.Clauses)
            {
                if (clause.Body is not Expr.Sequence sequence)
                {
                    continue;
                }

                Expr? binding = null;
                foreach (Expr statement in sequence.Items)
                {
                    if (ReferenceEquals(statement, consumer)
                        || statement is Expr.Match { Value: var value } && ReferenceEquals(value, consumer))
                    {
                        return binding ?? expression;
                    }

                    if (statement is Expr.Match
                        {
                            Pattern: Pattern.Variable assigned,
                            Value: var bound
                        } && assigned.Name == variable.Name)
                    {
                        binding = bound;
                    }
                }
            }

            return expression;
        }

        private const string FlattenListFunction = "flatten";

        private bool ProvesProperListReturn(
            Expr expression,
            HashSet<(string Name, int Arity)> active,
            bool includeFlatten = false
        )
        {
            if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
            {
                while (constant is Cons item)
                {
                    constant = item.Tail;
                }

                return constant is Nil;
            }

            if (expression is Expr.Call
                {
                    Module: ListModuleNames.Module, DynamicRemote: false, Arguments.Count: 1, Function: var function
                } && (function == SortListFunction || includeFlatten && function == FlattenListFunction))
            {
                return true;
            }

            if (expression is Expr.Call
                {
                    Function: AutoImportNames.Exit, DynamicRemote: false, Arguments.Count: 1
                } exit && owner.ResolvesErlangBif(exit))
            {
                return true;
            }

            if (expression is Expr.Case choice)
            {
                return choice.Clauses.All(clause => ProvesProperListReturn(clause.Body, active, includeFlatten));
            }

            if (expression is not Expr.Call { Module: null, DynamicRemote: false } local)
            {
                return false;
            }

            var signature = (local.Function, local.Arguments.Count);
            if (owner.IsDeclaredNative(signature) || !localFunctions.TryGetValue(signature, out var target))
            {
                return false;
            }

            if (!active.Add(signature))
            {
                return true;
            }

            bool proven = target.Clauses.All(clause => ProvesProperListReturn(clause.Body, active, includeFlatten));
            active.Remove(signature);

            return proven;
        }
    }
}
